using System.Runtime.CompilerServices;
using System.Text.Json;

namespace libthumbnailer2
{
    public class Config
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Width { get; set; }
        public int Gap { get; set; }
        public int BackgroundColor { get; set; }
        public int InfoFontColor { get; set; }
        public int TimeFontColor { get; set; }
        public int ShadowColor { get; set; }
        public string InfoFont { get; set; } = string.Empty;
        public string TimeFont { get; set; } = string.Empty;
        public int InfoFontSize { get; set; }
        public int TimeFontSize { get; set; }
        public bool PrintInfo { get; set; }
        public bool PrintTime { get; set; }
        public string ConfigPath { get; set; } = "config.json";
        public static Config CurrentConfig { get; private set; }

        private readonly string _defaultPath = "config.json";

        public Config()
        {
            // Empty constructor for JSON serilization...
        }

        public Config(bool loadDefault = true)
        {
            if (loadDefault)
                CurrentConfig = Load(_defaultPath).Result;
        }

        public async Task Save()
        {
            if (string.IsNullOrEmpty(ConfigPath))
                await SaveAs(_defaultPath);
            else
                await SaveAs(ConfigPath);
        }

        public async Task SaveAs(string path)
        {
            using FileStream stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, this);
            await stream.DisposeAsync();
        }

        public static async Task<Config> Load(string path)
        {
            using FileStream stream = File.OpenRead(path);
            Config? retval = await JsonSerializer.DeserializeAsync<Config>(stream);
            if(retval == null)
            {
                return new Config(true);
            }
            return retval;
        }
    }
}
