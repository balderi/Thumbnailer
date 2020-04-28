using System.Drawing;

namespace Thumbnailer
{
    class Thumbnail
    {
        public string Path { get; }
        public Image Image { get; }
        public double TimeCode { get; }

        public Thumbnail(string path, double timeCode)
        {
            Path = path;
            TimeCode = timeCode;
            Image = Image.FromFile(Path);
        }

        public Thumbnail(Bitmap bitmap, double timeCode)
        {
            Image = bitmap;
            TimeCode = timeCode;
        }

        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
