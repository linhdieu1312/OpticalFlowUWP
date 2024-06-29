using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using OpenCVComp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Win2DCustomEffects;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace CustomVideoEffects
{
    public sealed class OpticalFlowEffect : IBasicVideoEffect
    {
        private CanvasDevice _canvasDevice;
        private SoftwareBitmap _prevFrame;
        private OpenCVHelper _openCVHelper = new OpenCVHelper();
        
        private int CountFrames = 0;

        PixelShaderEffect effect;

        List<TrackingPoint> trackingPath;

        private async Task<byte[]> ReadAllBytes(string filename)
        {
            var uri = new Uri("ms-appx:///" + filename);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var buffer = await FileIO.ReadBufferAsync(file);

            return buffer.ToArray();
        }

        public async void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            try
            {
                _canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device);

                effect = new PixelShaderEffect(await ReadAllBytes("Assets/TileMosaic.bin"));
                effect.Properties["iResolution"] = new Vector2(encodingProperties.Width, encodingProperties.Height);
            }
            catch { }
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            try
            {
                CountFrames++;

                List<MaskInfo> inputPoints = null;
                object val;
                if (configuration != null && configuration.TryGetValue("Points", out val))
                {
                    inputPoints = val as List<MaskInfo>;
                }

                if (inputPoints == null || inputPoints.Count == 0 || inputPoints[0].FalseAlarmCount > 2)
                {
                    if (_prevFrame != null)
                    {
                        _prevFrame.Dispose();
                        _prevFrame = null;
                    }

                    return;
                }

                // Track 1 skip 1
                //if ((CountFrames - 1) % 2 == 0)
                {
                    SoftwareBitmap curFrame = null;
                        
                    Task.Run(
                        async () =>
                        {
                            try
                            {
                                curFrame = await SoftwareBitmap.CreateCopyFromSurfaceAsync(context.InputFrame.Direct3DSurface);
                            }
                            catch
                            { }

                        }).Wait();

                    if (curFrame == null) return;

                    int w = curFrame.PixelWidth;
                    int h = curFrame.PixelHeight;

                    if (_prevFrame == null)
                    {
                        _prevFrame = SoftwareBitmap.Copy(curFrame);
                    }

                    byte[] pointStatus = new byte[inputPoints.Count];   // inputPoints.Count should be 1
                    Point[] curPoint = new Point[inputPoints.Count];
                    for (int i = 0; i < curPoint.Length; i++)
                        curPoint[i] = new Point();

                    foreach (var p in inputPoints)
                    {
                        if (p.RealPoint.X == -1 && p.RealPoint.Y == -1)
                        {
                            p.RealPoint = new Point(p.PosX * w, p.PosY * h);
                        }
                    }

                    var prevPoints = inputPoints.Select(x => x.RealPoint).ToArray();

                    // Track 1 point at a time
                    _openCVHelper.Track(_prevFrame, curFrame, prevPoints, curPoint, pointStatus, inputPoints[0].MaximumIterations, inputPoints[0].Epsilon, inputPoints[0].WindowSize);

                    if (pointStatus[0] != 0)
                    {
                        inputPoints[0].RealPoint = new Point(curPoint[0].X, curPoint[0].Y);
                        Point normalizedPoint = new Point(curPoint[0].X / w, curPoint[0].Y / h);
                        trackingPath.Add(new TrackingPoint(normalizedPoint, context.InputFrame.RelativeTime.Value.TotalMilliseconds));
                    }
                    else
                    {
                        inputPoints[0].FalseAlarmCount++;
                    }

                    _prevFrame?.Dispose();
                    _prevFrame = curFrame;
                }

                if (_prevFrame == null)
                    return;

                // Draw results
                using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    var p = inputPoints[0];
                    if (p.FalseAlarmCount <= 2)
                    {
                        // Elipse mask
                        float cX = (float)p.RealPoint.X;
                        float cY = (float)p.RealPoint.Y;
                        float width = (float)p.UIWidth * inputBitmap.SizeInPixels.Width;
                        float height = (float)p.UIHeight * inputBitmap.SizeInPixels.Height;

                        CanvasGeometry mask = null;                                                                                                        
                        if (p.MaskShape == 0)       // Round
                        {
                            mask = CanvasGeometry.CreateEllipse(ds, cX, cY, width / 2f, height / 2f);
                        }
                        else                        // Rectangle
                        {
                            float X = cX - width / 2f;
                            float Y = cY - height / 2f;
                            mask = CanvasGeometry.CreateRectangle(ds, X, Y, width, height);
                        }

                        using (var layer = ds.CreateLayer(1f, mask))
                        {
                            if (p.MaskType == 1)    // Pixelate
                            {
                                effect.Properties["iTileSize"] = (float)p.BlurAmount;
                                effect.Source1 = inputBitmap;
                                ds.DrawImage(effect);
                            }
                            else                    // Blur
                            {
                                using (var blurEffect = new GaussianBlurEffect())
                                {
                                    blurEffect.BlurAmount = (float)p.BlurAmount;
                                    blurEffect.BorderMode = EffectBorderMode.Hard;
                                    blurEffect.Source = inputBitmap;
                                    ds.DrawImage(blurEffect);
                                }
                            }
                        }
                                
                        mask.Dispose();
                    }
                }
            }
            catch
            { }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            try
            {
                if (reason == MediaEffectClosedReason.Done)
                {
                    effect?.Dispose();
                    _prevFrame?.Dispose();
                    _canvasDevice?.Dispose();
                }                    
            }
            catch { }
        }

        public void DiscardQueuedFrames()
        {

        }

        public bool IsReadOnly => true;

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties => new List<VideoEncodingProperties>();

        public MediaMemoryTypes SupportedMemoryTypes => MediaMemoryTypes.Gpu;

        public bool TimeIndependent => false;

        private IPropertySet configuration;
        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;

            object val;
            if (configuration != null && configuration.TryGetValue("TrackingPath", out val))
            {
                trackingPath = val as List<TrackingPoint>;
            }
        }
    }
}
