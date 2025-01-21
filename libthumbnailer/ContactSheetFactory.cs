using Serilog;

namespace libthumbnailer
{
    public class ContactSheetFactory
    {
        public static ContactSheet CreateContactSheet(string filePath, Config config, ILogger logger)
        {
            return new ContactSheet(filePath, config, logger);
        }
    }
}
