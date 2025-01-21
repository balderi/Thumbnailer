using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;

namespace libthumbnailer
{
    public class FontFactory
    {
        public static Font CreateFont(FontFamily fontFamily, int fontSize)
        {
            if (fontSize <= 0)
            {
                fontSize = 12;
            }

            return new Font(fontFamily, fontSize, FontStyle.Bold);
        }
    }

    public class BrushFactory
    {
        public static SolidBrush CreateBrush(Color color)
        {
            return new SolidBrush(color);
        }
    }
}
