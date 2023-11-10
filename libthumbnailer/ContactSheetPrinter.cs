using System.Collections.Generic;

namespace libthumbnailer
{
    public static class ContactSheetPrinter
    {
        public static void PrintSingle(string path)
        {
            Logger logger = new Logger();
            List<ContactSheet> cs = new List<ContactSheet>()
            {
                ContactSheetFactory.CreateContactSheet(path, logger)
            };
            ContactSheet.PrintSheets(cs, Config.Load("default.xml"), logger, true);
            logger.Close();
        }

        public static void PrintMultiple(List<string> paths)
        {
            Logger logger = new Logger();
            List<ContactSheet> cs = new List<ContactSheet>();
            foreach (var p in paths)
            {
                ContactSheetFactory.CreateContactSheet(p, logger);
            }
            ContactSheet.PrintSheetsParallel(cs, Config.Load("default.xml"), logger, true);
            logger.Close();
        }
    }
}
