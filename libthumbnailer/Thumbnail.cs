using SixLabors.ImageSharp;

namespace libthumbnailer
{
    public class Thumbnail : IDisposable
    {
        public string Path { get; set; }
        public Image Image { get; set; }
        public double TimeCode { get; set; }

        public Thumbnail(string path, double timeCode)
        {
            Path = path;
            TimeCode = timeCode;
            Image = Image.Load(path);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Image.Dispose();
        }
    }
}
