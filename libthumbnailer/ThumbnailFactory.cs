using System.Threading.Tasks;

namespace libthumbnailer
{
    public class ThumbnailFactory
    {
        public static Thumbnail CreateThumbnail(string path, double timecode)
        {
            return new Thumbnail(path, timecode);
        }

        public static Task<ContactSheet> CreateContactSheet(string path, int rows, int cols, int width, int gap, Logger logger)
        {
            return new Task<ContactSheet>(() =>
                new ContactSheet(path, logger)
                {
                    Rows = rows,
                    Columns = cols,
                    Width = width,
                    Gap = gap
                });
        }
    }
}
