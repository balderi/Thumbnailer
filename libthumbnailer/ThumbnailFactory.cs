using SixLabors.ImageSharp;

namespace libthumbnailer
{
    /// <summary>
    /// Factory methods for creating <see cref="Thumbnail"/> instances.
    /// </summary>
    public class ThumbnailFactory
    {
        /// <summary>
        /// Initialize a new <see cref="Thumbnail"/> instance with the specified <paramref name="path"/> and <paramref name="timecode"/>.
        /// </summary>
        /// <param name="path">Path to the thumbnail.</param>
        /// <param name="timecode">Timecode of the thumbnail in seconds.</param>
        /// <returns>A new <see cref="Thumbnail"/> instance</returns>
        public static Thumbnail CreateThumbnail(string path, double timecode)
        {
            return new Thumbnail(path, timecode);
        }

        /// <summary>
        /// Initialize a new <see cref="Thumbnail"/> instance with the specified <paramref name="image"/> and <paramref name="timecode"/>.
        /// </summary>
        /// <param name="image">Thumbnail image.</param>
        /// <param name="timecode">Timecode of the thumbnail in seconds.</param>
        /// <returns>A new <see cref="Thumbnail"/> instance</returns>
        public static Thumbnail CreateThumbnail(Image image, double timecode)
        {
            return new Thumbnail(image, timecode);
        }
    }
}
