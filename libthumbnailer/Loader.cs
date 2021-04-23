using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace libthumbnailer
{
    public class FileLoadedEventArgs
    {
        public string FilePath { get; }
        public FileLoadedEventArgs(string filePath) { FilePath = filePath; }
    }

    public class Loader
    {
        static readonly string[] exts = new string[] { ".avi", ".mkv", ".wmv", ".mov", ".flv", ".divx", ".mp4", ".m4v", ".rm", ".mpg", ".mpeg", ".qt", ".webm" };

        public delegate void FileLoadedEventHandler(FileLoadedEventArgs e);

        public static event FileLoadedEventHandler FileLoadedEvent;

        /// <summary>
        /// Get one or more files in the specified path.
        /// </summary>
        /// <remarks>Can recurse though subfolders.</remarks>
        /// <param name="path">The path to fetch files from.</param>
        /// <param name="recursive">Whether or not to recurse through subfolders.</param>
        /// <returns>A collection of one or more file paths.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static IEnumerable<string> LoadFiles(string path, bool recursive = false)
        {
            if(string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Value cannot be empty or whitespace.", "path");
            }

            List<string> retval = new List<string>();

            if(File.Exists(path)) // path to single file
            {
                retval.Add(path);
            }
            else if(Directory.Exists(path)) // path to directory
            {
                retval.AddRange(GetFiles(path));
                if (recursive)
                {
                    retval.AddRange(GetDirs(path));
                }
            }
            else // invalid path
            {
                throw new ArgumentException("The specified file or folder does not exist.", "path");
            }

            return retval;
        }

        static IEnumerable<string> GetFiles(string path)
        {
            List<string> retval = new List<string>();

            foreach (string f in Directory.GetFiles(path))
            {
                FileInfo fi = new FileInfo(f);
                if (exts.Contains(fi.Extension.ToLower()))
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
