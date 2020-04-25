using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Thumbnailer
{
    class Config
    {
        [XmlElement(ElementName ="Columns")]
        public int Columns { get; set; }

        [XmlElement(ElementName = "Rows")]
        public int Rows { get; set; }

        [XmlElement(ElementName = "Width")]
        public int Width { get; set; }

        [XmlElement(ElementName = "Gap")]
        public int Gap { get; set; }

        public void Save()
        {

        }

        public void SaveAs()
        {

        }

        public void Load()
        {

        }
    }
}
