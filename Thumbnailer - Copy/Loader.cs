using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Thumbnailer
{
    public class Loader
    {
        static string[] exts = new string[] { ".avi", ".mkv", ".wmv", ".mov", ".flv", ".divx", ".mp4", ".m4v", ".rm", ".mpg", ".mpeg" };
        
        public static string[] LoadFiles(string path)
        {
            List<string> retval = new List<string>();

            retval.AddRange(GetFiles(path));
            retval.AddRange(GetDirs(path));

            return retval.ToArray();
        }

        static List<string> GetFiles(string path)
        {
            List<string> retval = new List<string>();

            foreach (string f in Directory.GetFiles(path))
            {
                FileInfo fi = new FileInfo(f);
                if(exts.Contains(fi.Extension) && !f.ToLower().Contains("sample"))
                {
                    retval.Add(f);
                }
            }

            return retval;
        }

        static List<string> GetDirs(string path)
        {
            List<string> retval = new List<string>();

            foreach (string d in Directory.GetDirectories(path))
            {
                try
                {
                    retval.AddRange(GetFiles(d));
                }
                catch { }
                try
                {
                    retval.AddRange(GetDirs(d));
                }
                catch { }
            }

            return retval;
        }
    }
}
