namespace libthumbnailer
{
    public class ContactSheetFactory
    {
        public static ContactSheet CreateContactSheet(string filePath, Logger logger)
        {
            return new ContactSheet(filePath, logger);
        }
    }
}
