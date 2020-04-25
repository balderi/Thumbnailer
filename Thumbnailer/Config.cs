using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Thumbnailer
{
    class Config
    {
        [XmlElement(ElementName = "Rows")]
        public int Rows { get; set; }

        [XmlElement(ElementName ="Columns")]
        public int Columns { get; set; }

        [XmlElement(ElementName = "Width")]
        public int Width { get; set; }

        [XmlElement(ElementName = "Gap")]
        public int Gap { get; set; }

        [XmlElement(ElementName = "BackgroundColor")]
        public int BackgroundColor { get; set; }

        [XmlElement(ElementName = "InfoColor")]
        public int InfoColor { get; set; }

        [XmlElement(ElementName = "TimeColor")]
        public int TimeColor { get; set; }

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
        public bool InfoChecked { get; set; }

        [XmlElement(ElementName = "TimeChecked")]
        public bool TimeChecked { get; set; }

        [XmlElement(ElementName = "ShadowChecked")]
        public bool ShadowChecked { get; set; }

        readonly string _defaultPath = "config.xml";

        public void Save(Config config)
        {
            SaveAs(config, _defaultPath);
        }

        public void SaveAs(Config config, string path)
        {
            var xmls = new XmlSerializer(config.GetType());
            var writer = new StreamWriter(path);
            xmls.Serialize(writer, config);
            writer.Close();
        }

        public static Config Load(string path)
        {
            var fs = new FileStream(path, FileMode.Open);
            var xmls = new XmlSerializer(typeof(Config));
            return (Config)xmls.Deserialize(fs);
        }
    }
}
