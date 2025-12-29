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
        
        public Thumbnail(Image image, double timeCode)
        {
            Path = string.Empty;
            TimeCode = timeCode;
            Image = image;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Image.Dispose();
        }
    }
}
