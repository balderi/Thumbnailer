using System;
using System.Drawing;

namespace libthumbnailer
{
    /// <summary>
    /// Class for holding data about individual thumbnails.
    /// </summary>
    public class Thumbnail : IDisposable
    {
        public string Path { get; }
        public Image Image { get; }
        public double TimeCode { get; }

        /// <summary>
        /// Creates a new <see cref="Thumbnail"/> instance.
        /// </summary>
        /// <param name="path">Path to the thumbnail.</param>
        /// <param name="timeCode">Time code of the tumbnail in seconds.</param>
        /// <remarks>Generates an <see cref="System.Drawing.Image"/> of the file specified in <paramref name="path"/>.</remarks>
        public Thumbnail(string path, double timeCode)
        {
            Path = path;
            TimeCode = timeCode;
            Image = Image.FromFile(Path);
        }

        /// <summary>
        /// Dispose this <see cref="Thumbnail"/>.
        /// </summary>
        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
