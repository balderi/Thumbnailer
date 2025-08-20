using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace libthumbnailer
{
    public static class ContactSheetPrinter
    {
        public static bool PrintSingle(string path)
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

            try
            {
                var sheet = ContactSheetFactory.CreateContactSheet(path, config, Log.Logger);
                return sheet.PrintSheet(true);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
