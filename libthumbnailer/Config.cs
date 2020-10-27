using System.IO;
using System.Xml.Serialization;

namespace libthumbnailer
{
    public class Config
    {
        [XmlElement(ElementName = "Rows")]
        public int Rows { get; set; }

        [XmlElement(ElementName = "Columns")]
        public int Columns { get; set; }

        [XmlElement(ElementName = "Width")]
        public int Width { get; set; }

        [XmlElement(ElementName = "Gap")]
        public int Gap { get; set; }

        [XmlElement(ElementName = "BackgroundColor")]
        public int BackgroundColor { get; set; }

        [XmlElement(ElementName = "InfoColor")]
        public int InfoFontColor { get; set; }

        [XmlElement(ElementName = "TimeColor")]
        public int TimeFontColor { get; set; }

        [XmlElement(ElementName = "ShadowColor")]
        public int ShadowColor { get; set; }

        [XmlElement(ElementName = "InfoFont")]
        public string InfoFont { get; set; }

        [XmlElement(ElementName = "TimeFont")]
        public string TimeFont { get; set; }

        [XmlElement(ElementName = "InfoFontSize")]
        public int InfoFontSize { get; set; }

        [XmlElement(ElementName = "TimeFontSize")]
        public int TimeFontSize { get; set; }

        [XmlElement(ElementName = "InfoChecked")]
        public bool PrintInfo { get; set; }

        [XmlElement(ElementName = "TimeChecked")]
        public bool PrintTime { get; set; }

        public string ConfigPath { get; set; }

        public static Config CurrentConfig { get; private set; }

        private readonly string _defaultPath = "config.xml";

        private Config()
        {
            // Parameterless constructor for XML serialization
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
            var xmls = new XmlSerializer(this.GetType());
            var writer = new StreamWriter(path);
            xmls.Serialize(writer, this);
            writer.Close();
        }

        public static Config Load(string path)
        {
            var fs = new FileStream(path, FileMode.Open);
            var xmls = new XmlSerializer(typeof(Config));
            var retval = (Config)xmls.Deserialize(fs);
            fs.Close();
            retval.ConfigPath = path;
            CurrentConfig = retval;
            return retval;
        }
    }
}
