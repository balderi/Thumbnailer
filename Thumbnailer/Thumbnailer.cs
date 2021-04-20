using System.Collections.Generic;
using libthumbnailer;

namespace Thumbnailer
{
    class Thumbnailer
    {
        List<ContactSheet> ContactSheets { get; set; }
        int Rows { get; set; }
        int Columns { get; set; }
        int Width { get; set; }
        int Gap { get; set; }
        bool SameDir { get; set; }

        public Thumbnailer(List<ContactSheet> contactSheets, int rows, int cols, int width, int gap)
        {

        }


    }
}
