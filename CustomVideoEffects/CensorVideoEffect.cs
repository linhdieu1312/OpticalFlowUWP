using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Win2DCustomEffects;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace CustomVideoEffects
{
    public sealed class CensorVideoEffect : IBasicVideoEffect
    {
        List<MaskInfo> masks = null;
        PixelShaderEffect effect;
        float cX, cY;

        public bool IsReadOnly
        {
            get { return true; }
        }

        public IReadOnlyList<VideoEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var encodingProperties = new VideoEncodingProperties();
                encodingProperties.Subtype = "ARGB32";
                return new List<VideoEncodingProperties>() { encodingProperties };
            }
        }

        public MediaMemoryTypes SupportedMemoryTypes
        {
            get { return MediaMemoryTypes.Gpu; }
        }

        public bool TimeIndependent
        {
            get { return true; }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            try
            {
                if (reason == MediaEffectClosedReason.Done)
                {
                    effect?.Dispose();
                    _canvasDevice?.Dispose();
                }                    
            }
            catch { }
        }

        public void DiscardQueuedFrames()
        {
            
        }

        public void ProcessFrame(ProcessVideoFrameContext context)
        {
            try
            {
                if (masks == null || masks.Count == 0)
                    return;

                double elapsedTime = context.InputFrame.RelativeTime.Value.TotalMilliseconds;

                using (CanvasBitmap inputBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, context.InputFrame.Direct3DSurface))
                using (CanvasRenderTarget renderTarget = CanvasRenderTarget.CreateFromDirect3D11Surface(_canvasDevice, context.OutputFrame.Direct3DSurface))
                using (CanvasDrawingSession ds = renderTarget.CreateDrawingSession())
                {
                    for (int j = 0; j < masks.Count; j++)
                    {
                        var mask = masks[j];

                        if (elapsedTime < mask.StartTime || elapsedTime > mask.StopTime)
                            continue;

                        if (mask.IsTrackingMask)
                        {
                            // Neu tracking path la null thi hien thi theo kieu static mask
                            if (mask.TrackingPath == null || mask.TrackingPath.Count < 2)
                            {
                                cX = (float)mask.PosX * inputBitmap.SizeInPixels.Width;
                                cY = (float)mask.PosY * inputBitmap.SizeInPixels.Height;
                            }
                            else
                            {
                                if (elapsedTime >= mask.StartTime && elapsedTime < mask.TrackingPath[0].RelativeTime)
                                {
                                    cX = (float)mask.PosX * inputBitmap.SizeInPixels.Width;
                                    cY = (float)mask.PosY * inputBitmap.SizeInPixels.Height;
                                }
                                else if (elapsedTime <= mask.StopTime && elapsedTime > mask.TrackingPath[mask.TrackingPath.Count - 1].RelativeTime)
                                {
                                    // cY, cY keep the previous values
                                }
                                else
                                {
                                    // Get position from tracking path
                                    int i = 0;
                                    for (; i < mask.TrackingPath.Count - 1; i++)
                                    {
                                        if (mask.TrackingPath[i].RelativeTime <= elapsedTime && mask.TrackingPath[i + 1].RelativeTime > elapsedTime)
                                            break;
                                    }

                                    cX = (float)mask.TrackingPath[i].RealPoint.X * inputBitmap.SizeInPixels.Width;
                                    cY = (float)mask.TrackingPath[i].RealPoint.Y * inputBitmap.SizeInPixels.Height;
                                }                                
                            }
                        }
                        else
                        {
                            cX = (float)mask.PosX * inputBitmap.SizeInPixels.Width;
                            cY = (float)mask.PosY * inputBitmap.SizeInPixels.Height;
                        }
                        
                        float width = (float)mask.UIWidth * inputBitmap.SizeInPixels.Width;
                        float height = (float)mask.UIHeight * inputBitmap.SizeInPixels.Height;

                        CanvasGeometry canvasGeometry = null;
                        if (mask.MaskShape == 0)        // Round
                        {
                            canvasGeometry = CanvasGeometry.CreateEllipse(ds, cX, cY, width / 2f, height / 2f);
                        }
                        else                            // Rectangle
                        {
                            float X = cX - width / 2f;
                            float Y = cY - height / 2f;
                            canvasGeometry = CanvasGeometry.CreateRectangle(ds, X, Y, width, height);
                        }

                        using (var layer = ds.CreateLayer(1f, canvasGeometry))
                        {
                            if (mask.MaskType == 1)    // Pixelate
                            {
                                effect.Properties["iTileSize"] = (float)mask.BlurAmount;
                                effect.Source1 = inputBitmap;
                                ds.DrawImage(effect);
                            }
                            else                        // Blur
                            {
                                using (var blurEffect = new GaussianBlurEffect())
                                {
                                    blurEffect.BlurAmount = (float)mask.BlurAmount;
                                    blurEffect.BorderMode = EffectBorderMode.Hard;
                                    blurEffect.Source = inputBitmap;
                                    ds.DrawImage(blurEffect);
                                }
                            }
                        }

                        canvasGeometry.Dispose();
                    }                    
                }
            }
            catch
            { }
        }

        private async Task<byte[]> ReadAllBytes(string filename)
        {
            var uri = new Uri("ms-appx:///" + filename);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var buffer = await FileIO.ReadBufferAsync(file);

            return buffer.ToArray();
        }

        private CanvasDevice _canvasDevice;
        public async void SetEncodingProperties(VideoEncodingProperties encodingProperties, IDirect3DDevice device)
        {
            _canvasDevice = CanvasDevice.CreateFromDirect3D11Device(device);

            if (effect == null)
            {
                effect = new PixelShaderEffect(await ReadAllBytes("Assets/Shaders/TileMosaic.bin"));
                effect.Properties["iResolution"] = new Vector2(encodingProperties.Width, encodingProperties.Height);
            }            
        }

        private IPropertySet configuration;
        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;

            object val;
            if (configuration != null && configuration.TryGetValue("Masks", out val))
            {
                masks = val as List<MaskInfo>;
            }
        }
    }
}
