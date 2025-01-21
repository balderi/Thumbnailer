using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml.Serialization;

namespace libthumbnailer
{
    public class Config
    {
        public int Rows { get; set; }

        public int Columns { get; set; }

        public int Width { get; set; }

        public int Gap { get; set; }

        public string BackgroundColor { get; set; } = string.Empty;

        public string InfoFontColor { get; set; } = string.Empty;

        public string TimeFontColor { get; set; } = string.Empty;

        public string ShadowColor { get; set; } = string.Empty;

        public string InfoFont { get; set; } = string.Empty;

        public string TimeFont { get; set; } = string.Empty;

        public int InfoFontSize { get; set; }

        public int TimeFontSize { get; set; }

        public bool PrintInfo { get; set; }

        public bool PrintTime { get; set; }

        public string ConfigPath { get; set; } = "default.json";

        [JsonIgnore]
        public static Config? CurrentConfig { get; private set; }

        [JsonIgnore]
        private readonly string _defaultPath = "default.json";

        [JsonConstructor]
        public Config()
        {
            
        }
        public Config(bool loadDefault = false)
        {
            if (loadDefault)
                CurrentConfig = Load(_defaultPath);
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(ConfigPath))
                SaveAs(_defaultPath);
            else
                SaveAs(ConfigPath);
        }

        public void SaveAs(string path)
        {
            ConfigPath = path;
            var writer = File.OpenWrite(path);
            var options = new JsonSerializerOptions { WriteIndented = true };
            JsonSerializer.Serialize(writer, this, options);
            writer.Close();
        }

        public static Config Load(string path = "")
        {
            if(string.IsNullOrEmpty(path))
            {
                path = "default.json";
            }
            var json = File.ReadAllText(path);
            var retval = JsonSerializer.Deserialize<Config>(json) ?? new Config();
            retval.ConfigPath = path;
            CurrentConfig = retval;
            return retval;
        }
    }
}
