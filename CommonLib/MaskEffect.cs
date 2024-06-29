using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Win2DCustomEffects
{
    public class MaskObject
    {
        public double X;
        public double Y;
        public double Width;
        public double Height;
        public int Shape;
        public double BlurAmount;
        public double StartTime;
        public double Duration;
    }

    public class MaskEffect
    {
        private List<MaskObject> _masks;
        private double _inputVideoRatio = 0;

        PixelShaderEffect effect;

        private async Task<byte[]> ReadAllBytes(string filename)
        {
            var uri = new Uri("ms-appx:///" + filename);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var buffer = await FileIO.ReadBufferAsync(file);

            return buffer.ToArray();
        }

        public async void SetProperties(IPropertySet configuration)
        {
            try
            {
                object val;
                if (configuration != null && configuration.TryGetValue("Masks", out val))
                {
                    string json = (string)val;
                    SetMaskData(json);
                }

                if (configuration != null && configuration.TryGetValue("InputVideoRatio", out val))
                    _inputVideoRatio = (double)val;
                else
                    _inputVideoRatio = 0;

                effect = new PixelShaderEffect(await ReadAllBytes("Assets/Shaders/TileMosaic.bin"));
                effect.Properties["iTileSize"] = 16f;
            }
            catch { }
        }

        private void SetMaskData(string json)
        {
            if (string.IsNullOrEmpty(json))
                _masks = null;
            else
            {
                try
                {
                    _masks = JsonConvert.DeserializeObject<List<MaskObject>>(json);
                }
                catch
                {
                    _masks = null;
                }
            }
        }

        public void ShowMasks(IPropertySet configuration)
        {
            try
            {
                object val;
                if (configuration != null && configuration.TryGetValue("Masks", out val))
                {
                    string json = (string)val;
                    SetMaskData(json);
                }
            }
            catch { }
        }

        public void HideMasks()
        {
            try
            {
                _masks = null;
            }
            catch { }
        }

        public void ShowEffect(CanvasDrawingSession ds, CanvasBitmap inputBitmap, double elapsedTime, double videoWidth, double videoHeight, int maskType)
        {
            try
            {
                double scaleWidth = videoWidth, scaleHeight = videoHeight;
                double dx = 0, dy = 0;

                if (_inputVideoRatio != 0)
                {
                    if (_inputVideoRatio < videoWidth / videoHeight)
                    {
                        scaleWidth = scaleHeight * _inputVideoRatio;
                        dx = (videoWidth - scaleWidth) / 2;
                    }
                    else
                    {
                        scaleHeight = scaleWidth / _inputVideoRatio;
                        dy = (videoHeight - scaleHeight) / 2;
                    }
                }

                if (_masks != null)
                    foreach (var mask in _masks)
                        if (mask.StartTime + mask.Duration > elapsedTime && mask.StartTime <= elapsedTime)
                        {
                            float maskWidth = (float)(mask.Width * scaleWidth);
                            float maskHeight = (float)(mask.Height * scaleHeight);

                            CanvasGeometry geometryMask = null;
                            if (mask.Shape == 1)    // Ellipse
                            {
                                float cX = (float)(mask.X * scaleWidth + maskWidth / 2 + dx);
                                float cY = (float)(mask.Y * scaleHeight + maskHeight / 2 + dy);
                                geometryMask = CanvasGeometry.CreateEllipse(ds, cX, cY, maskWidth / 2, maskHeight / 2);
                            }
                            else                    // Rectangle
                            {
                                float X = (float)(mask.X * scaleWidth + dx);
                                float Y = (float)(mask.Y * scaleHeight + dy);
                                geometryMask = CanvasGeometry.CreateRectangle(ds, X, Y, maskWidth, maskHeight);
                            }

                            using (var layer1 = ds.CreateLayer(1f, geometryMask))
                            {
                                if (maskType == 1)  // Pixelate
                                {
                                    effect.Properties["iResolution"] = new Vector2((float)videoWidth, (float)videoHeight);
                                    effect.Source1 = inputBitmap;
                                    ds.DrawImage(effect);
                                }
                                else                // Blur
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
                        }

                //if (_masks != null)
                //{
                //    int i = 0;
                //    for (; i < _masks.Count - 1; i++)
                //        if (_masks[i].StartTime <= elapsedTime && _masks[i + 1].StartTime > elapsedTime)
                //            break;
                //    if (i < _masks.Count - 1)
                //    {
                //        float ratio = (float)(elapsedTime - _masks[i].StartTime) / (float)(_masks[i + 1].StartTime - _masks[i].StartTime);

                //        float x1 = (float)(_masks[i].X * scaleWidth + dx);
                //        float y1 = (float)(_masks[i].Y * scaleHeight + dy);
                //        float w1 = (float)(_masks[i].Width * scaleWidth);
                //        float h1 = (float)(_masks[i].Height * scaleHeight);

                //        float x2 = (float)(_masks[i + 1].X * scaleWidth + dx);
                //        float y2 = (float)(_masks[i + 1].Y * scaleHeight + dy);
                //        float w2 = (float)(_masks[i + 1].Width * scaleWidth);
                //        float h2 = (float)(_masks[i + 1].Height * scaleHeight);

                //        float x = (x2 - x1) * ratio + x1;
                //        float y = (y2 - y1) * ratio + y1;
                //        float w = (w2 - w1) * ratio + w1;
                //        float h = (h2 - h1) * ratio + h1;

                //        using (var mask = CanvasGeometry.CreateRectangle(ds, x, y, w, h))
                //        using (var layer1 = ds.CreateLayer(1f, mask))
                //        using (var blurEffect = new GaussianBlurEffect())
                //        {
                //            blurEffect.BlurAmount = (float)_masks[i].BlurAmount;
                //            blurEffect.BorderMode = EffectBorderMode.Hard;
                //            blurEffect.Source = inputBitmap;
                //            ds.DrawImage(blurEffect);
                //        }
                //    }
                //}
            }
            catch { }
        }
    }
}
