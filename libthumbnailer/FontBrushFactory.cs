using System.Drawing;

namespace libthumbnailer
{
    public class FontFactory
    {
        public static Font CreateFont(FontFamily fontFamily, int fontSize)
        {
            if(fontSize <= 0)
            {
                fontSize = 12;
            }
            if(fontFamily == null)
            {
                var fc = new System.Drawing.Text.PrivateFontCollection();
                fc.AddFontFile("consola.ttf");
                fontFamily = new FontFamily("consolas");
            }

            return new Font(fontFamily, fontSize, FontStyle.Bold, GraphicsUnit.Point);
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
