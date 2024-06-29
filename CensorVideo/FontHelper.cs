using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CensorVideo
{
    public static class FontHelper
    {
        public static List<string> GetSystemFonts()
        {
            var fontFamilies = new List<string>();
            foreach (var font in CanvasTextFormat.GetSystemFontFamilies())
            {
                fontFamilies.Add(font);
            }

            return fontFamilies;
        }
    }
}
