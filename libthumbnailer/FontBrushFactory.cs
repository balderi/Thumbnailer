using System.Drawing;

namespace libthumbnailer
{
    public class FontFactory
    {
        public static Font CreateFont(FontFamily fontFamily, int fontSize)
        {
            return new Font(fontFamily, fontSize);
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
