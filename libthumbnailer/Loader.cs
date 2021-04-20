using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace libthumbnailer
{
    public class FileLoadedEventArgs
    {
        public string FilePath { get; }
        public FileLoadedEventArgs(string filePath) { FilePath = filePath; }
    }

    public class Loader
    {
        static readonly string[] exts = new string[] { ".avi", ".mkv", ".wmv", ".mov", ".flv", ".divx", ".mp4", ".m4v", ".rm", ".mpg", ".mpeg", ".qt" };

        public delegate void FileLoadedEventHandler(FileLoadedEventArgs e);

        public static event FileLoadedEventHandler FileLoadedEvent;

        public static IEnumerable<string> LoadFiles(string path, bool recursive = true)
        {
            List<string> retval = new List<string>();

            if(File.Exists(path)) // path to single file
            {
                retval.Add(path);
            }
            else // path to directory
            {
                retval.AddRange(GetFiles(path));
                if(recursive)
                {
                    retval.AddRange(GetDirs(path));
                }
            }

            return retval;
        }

        static IEnumerable<string> GetFiles(string path)
        {
            List<string> retval = new List<string>();

            foreach (string f in Directory.GetFiles(path))
            {
                FileInfo fi = new FileInfo(f);
                if (exts.Contains(fi.Extension))
                {
                    retval.Add(f);
                    FileLoadedEvent?.Invoke(new FileLoadedEventArgs(f));
                }
            }

            return retval;
        }

        static IEnumerable<string> GetDirs(string path)
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
