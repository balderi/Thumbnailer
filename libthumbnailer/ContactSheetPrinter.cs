using Microsoft.Extensions.Configuration;
using Serilog;

namespace libthumbnailer
{
    public static class ContactSheetPrinter
    {
        public static void PrintSingle(string path)
        {
            Config config;

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("thumbsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();


            if (Config.CurrentConfig is null)
            {
                config = Config.Load("default.json");
            }
            else
            {
                config = Config.CurrentConfig;
            }

            var sheet = ContactSheetFactory.CreateContactSheet(path, config, Log.Logger);

            sheet.PrintSheet(true);
        }
    }
}
