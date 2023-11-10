using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;

namespace libthumbnailer
{
    public class ContactSheet
    {
        public string FilePath { get; private set; }
        public double Duration { get; private set; }
        public string FileInfo { get; private set; }
        public string AudioInfo { get; private set; }
        public string VideoInfo { get; private set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Width { get; set; }
        public int Height { get; private set; }
        public int Gap { get; set; }
        public List<Thumbnail> Thumbnails { get; }

        private readonly Logger _logger;
        private readonly int _aspectRatio = 1;
        private string _thumbDir;

        public static event EventHandler<string> SheetCreated;
        public event EventHandler<string> SheetPrinted;
        public static event EventHandler<string> AllSheetsPrinted;

        public ContactSheet(string filePath, Logger logger)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _logger = logger;
            FilePath = filePath;
            var info = GetRootInfo();
            Duration = GetDuration(info);
            FileInfo = GetFileInfo(info);
            VideoInfo = GetVideoInfo(info);
            AudioInfo = GetAudioInfo(info);

            Thumbnails = new List<Thumbnail>();
            Height = 0;

            _logger.LogInfo($"--- Processing {filePath} ---");
        }

        private JsonElement GetRootInfo()
        {
            var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-show_streams -show_format -print_format json \"{FilePath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            string output;
            try
            {
                probe.Start();
                output = probe.StandardOutput.ReadToEnd().Trim();
                probe.WaitForExit();
                probe.Dispose();
            }
            catch
            {
                _logger.LogError($"ffprobe failed to start.");
                throw new FfprobeException();
            }

            JsonDocument doc = JsonDocument.Parse(output);
            return doc.RootElement;
        }

        private double GetDuration(JsonElement root)
        {
            try
            {
                return root.TryGetProperty("format", out var format) ? double.Parse(format.GetProperty("duration").GetString()) : -1;
            }
            catch
            {
                return -1;
            }
        }

        private string GetFileInfo(JsonElement root)
        {
            try
            {
                return root.TryGetProperty("format", out var format) ? Utils.GetFileInfo(format) : "Unknown file format";
            }
            catch
            {
                return "Unknown file format";
            }
        }

        private string GetVideoInfo(JsonElement root)
        {
            try
            {
                return TryGetIndex(root.GetProperty("streams"), "video", out int vindex) ?
                       Utils.GetVideoInfo(root.GetProperty("streams")[vindex]) : "Unknown video";
            }
            catch
            {
                return "Unknown video";
            }
        }

        private string GetAudioInfo(JsonElement root)
        {
            try
            {
                return TryGetIndex(root.GetProperty("streams"), "audio", out int aindex) ?
                       Utils.GetAudioInfo(root.GetProperty("streams")[aindex]) : "Unknown audio";
            }
            catch
            {
                return "Unknown audio";
            }
        }

        //private void TryCalculateRatio(JsonElement root)
        //{
        //    // If the file has some weird aspect ratio - like 2:1 - the thumbnails will be distorted
        //    // So we check for it and adjust accordingly elsewhere - otherwise the ratio is initialized as 1
        //    if (TryGetIndex(root.GetProperty("streams"), "video", out int vindex) && vindex > 0)
        //    {
        //        if (root.GetProperty("streams")[vindex].TryGetProperty("sample_aspect_ratio", out var aspect))
        //        {
        //            int w = int.Parse(aspect.GetString().Split(':')[0]);
        //            int h = int.Parse(aspect.GetString().Split(':')[1]);
        //            if (w > h)
        //                _aspectRatio = w / h;
        //        }
        //    }
        //}

        private bool TryGetIndex(JsonElement streams, string streamName, out int index)
        {
            int len = streams.GetArrayLength();
            index = -1;

            if (len < 1)
                return false;

            for (int i = 0; i < len; i++)
            {
                if (streams[i].TryGetProperty("codec_type", out var codecName) && codecName.GetString() == streamName)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        //Generates thumbnails by running ffmpeg on the file, and saving them in a temp folder.
        private void GenerateThumbnails()
        {
            double tween = Duration / (Rows * Columns);

            _thumbDir = "temp/temp_" + (FilePath.GetHashCode() + DateTime.Now.Millisecond);
            Directory.CreateDirectory(_thumbDir);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{FilePath}\" -vf fps=1/{tween} {_thumbDir}/img%05d.png",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            //try
            //{
                proc.Start();
                proc.WaitForExit();
                proc.Dispose();
            //}
            //catch
            //{
            //    _logger.LogError($"ffmpeg failed to start.");
            //    throw new FfmpegException();
            //}

            int count = 0;

            var holder = Directory.GetFiles(_thumbDir);
            // Sort the list of screenshots lexicographically, because linux reads files weird, apparently...
            var sorted = from s in holder orderby s select s;

            foreach (string s in sorted)
            {
                Thumbnails.Add(ThumbnailFactory.CreateThumbnail(s, ++count * tween));
            }
        }

        private string PrintInfo()
        {
            return "File: " + new FileInfo(FilePath).Name + 
                            Environment.NewLine + FileInfo + 
                            Environment.NewLine + AudioInfo + 
                            Environment.NewLine + VideoInfo;
        }

        /// <summary>
        /// Prints the contact sheet to the file specified in <paramref name="filename"/>.
        /// </summary>
        /// <param name="filename">The file name of the contact sheet</param>
        /// <param name="config">The configuration to use when printing.</param>
        /// <param name="overwrite">Whether or not to overwrite an existing file.</param>
        /// <returns>True if no errors occur; otherwise false.</returns>
        public bool PrintSheet(string filename, Config config, bool overwrite)
        {
            if (!overwrite && File.Exists(filename + ".png"))
            {
                _logger.LogInfo($"SKIP: {filename} already exists - skipping");
                return true;
            }

            GenerateThumbnails();

            int imgWidth = (Width - ((Columns - 1) * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width;
            double th = Thumbnails[0].Image.Height;
            double ratio = th / tw;
            int imgHeight = (int)Math.Round(ratio * imgWidth / _aspectRatio);
            Height = (imgHeight * Rows) + 2 + (Rows * Gap);
            int infoHeight = 0;

            Font infoF = FontFactory.CreateFont(Utils.GetFontFamilyFromName(config.InfoFont), config.InfoFontSize);
            Font timeF = FontFactory.CreateFont(Utils.GetFontFamilyFromName(config.TimeFont), config.TimeFontSize);
            SolidBrush infoB = BrushFactory.CreateBrush(Color.FromArgb(config.InfoFontColor));
            SolidBrush timeB = BrushFactory.CreateBrush(Color.FromArgb(config.TimeFontColor));
            SolidBrush timeSB = BrushFactory.CreateBrush(Color.FromArgb(config.ShadowColor));

            if (config.PrintInfo)
            {
                infoHeight = Utils.GetStringHeight(PrintInfo(), infoF);
                Height += infoHeight;
            }

            using (var bitmap = new Bitmap(Width, Height))
            {
                using (var canvas = Graphics.FromImage(bitmap))
                {
                    canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    canvas.SmoothingMode = SmoothingMode.HighQuality;
                    canvas.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    canvas.CompositingQuality = CompositingQuality.HighQuality;
                    canvas.Clear(Color.FromArgb(config.BackgroundColor));
                    int idx = 0;

                    for (int i = 0; i < Rows; i++)
                    {
                        for (int j = 0; j < Columns; j++)
                        {
                            int x = 2 + (j * (imgWidth + Gap));
                            int y = infoHeight + (i * (imgHeight + Gap));
                            try
                            {
                                canvas.DrawImage(Thumbnails[idx].Image, new Rectangle(x, y, imgWidth, imgHeight));
                            }
                            catch (Exception e)
                            {
                                _logger.LogWarning($"({FilePath}): {e.Message}");
                                _logger.LogWarning($"Expecting {Rows * Columns} images, recieved {Thumbnails.Count}");
                            }
                            if (config.PrintTime)
                            {
                                string time = "";
                                try
                                {
                                    time = Converter.ToHMS(Thumbnails[idx].TimeCode);
                                }
                                catch (Exception e)
                                {
                                    time = "?";
                                    _logger.LogWarning($"TIMECODE ({FilePath}): {e.Message}");
                                }
                                int timeWidth = Utils.GetStringWidth(time, timeF, canvas);
                                int timeHeight = Utils.GetStringHeight(time, timeF, canvas);
                                canvas.DrawString(time, timeF, timeSB, new PointF(x + imgWidth - timeWidth + 1, y + imgHeight - timeHeight + 1));
                                canvas.DrawString(time, timeF, timeB, new PointF(x + imgWidth - timeWidth, y + imgHeight - timeHeight));
                            }
                            idx++;
                        }
                    }

                    if (config.PrintInfo)
                        canvas.DrawString(PrintInfo(), infoF, infoB, new PointF(2, 2));

                    canvas.Save();
                }
                try
                {
                    bitmap.Save(filename + ".png", ImageFormat.Png);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unable to save file {filename}: {e.Message}");
                    return false;
                }
            }

            foreach (var t in Thumbnails)
            {
                t.Dispose();
                try
                {
                    File.Delete(t.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete file {t.Path}: {ex.Message}");
                }
            }
            try
            {
                Directory.Delete(_thumbDir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to delete directory {_thumbDir}: {ex.Message}");
            }

            SheetPrinted?.Invoke(this, FilePath);
            return true;
        }

        public static List<ContactSheet> BuildSheets(IEnumerable<string> files, Logger logger)
        {
            var retval = new List<ContactSheet>();

            foreach (string file in files)
            {
                try
                {
                    retval.Add(ContactSheetFactory.CreateContactSheet(file, logger));
                    SheetCreated?.Invoke(null, file);
                }
                catch (Exception e)
                {
                    logger.LogError($"Exception during sheet-building of file {file}: {e.Message}");
                    continue;
                }
            }

            return retval;
        }

        /// <summary>
        /// Prints a list of contact sheets to file(s).
        /// </summary>
        /// <param name="sheets">The list of contact sheets to print.</param>
        /// <param name="config">The configuration to use when printing.</param>
        /// <param name="logger">The <see cref="Logger"/> instance to use for logging.</param>
        /// <param name="overwrite">Whether or not to overwrite existing files.</param>
        /// <param name="outputPath">The path to print files to, if it is different from the source path.</param>
        /// <remarks>Prints the contact sheets sequentially.</remarks>
        public static void PrintSheets(List<ContactSheet> sheets, Config config, Logger logger, bool overwrite, string outputPath = null)
        {
            var start = DateTime.Now;
            var results = new List<bool>();

            foreach (ContactSheet cs in sheets)
            {
                string filePath;
                cs.Rows = Config.CurrentConfig.Rows;
                cs.Columns = Config.CurrentConfig.Columns;
                cs.Width = Config.CurrentConfig.Width;
                cs.Gap = Config.CurrentConfig.Gap;

                if (string.IsNullOrEmpty(outputPath))
                {
                    filePath = cs.FilePath;
                }
                else
                {
                    filePath = outputPath + "/" + new FileInfo(cs.FilePath).Name;
                }

                try
                {
                    results.Add(cs.PrintSheet(filePath, config, overwrite));
                }
                catch(Exception e)
                {
                    logger.LogError($"Exception caught while printing sheet: {filePath}: {e.Message}\n{e.StackTrace}");
                    results.Add(false);
                }
            }

            AllSheetsPrinted?.Invoke(null, "All done!");
            logger.LogInfo($"*** Done in {DateTime.Now.Subtract(start).TotalSeconds} seconds ***");
        }

        /// <summary>
        /// Prints a list of contact sheets to file(s).
        /// </summary>
        /// <param name="sheets">The list of contact sheets to print.</param>
        /// <param name="config">The configuration to use when printing.</param>
        /// <param name="logger">The <see cref="Logger"/> instance to use for logging.</param>
        /// <param name="overwrite">Whether or not to overwrite existing files.</param>
        /// <param name="outputPath">The path to print files to, if it is different from the source path.</param>
        /// <remarks>Prints the contact sheets parallellized (multi-threaded).</remarks>
        public static void PrintSheetsParallel(List<ContactSheet> sheets, Config config, Logger logger, bool overwrite, string outputPath = null)
        {
            var start = DateTime.Now;
            List<bool> results = new List<bool>();

            Parallel.ForEach(sheets, cs =>
            {
                string filePath;
                cs.Rows = Config.CurrentConfig.Rows;
                cs.Columns = Config.CurrentConfig.Columns;
                cs.Width = Config.CurrentConfig.Width;
                cs.Gap = Config.CurrentConfig.Gap;

                if (string.IsNullOrEmpty(outputPath))
                {
                    filePath = cs.FilePath;
                }
                else
                {
                    filePath = outputPath + "/" + new FileInfo(cs.FilePath).Name;
                }

                try
                {
                    results.Add(cs.PrintSheet(filePath, config, overwrite));
                }
                catch (Exception e)
                {
                    logger.LogError($"Exceprion caught while printing sheet: {filePath}: {e.Message}");
                    results.Add(false);
                }
            });

            AllSheetsPrinted?.Invoke(null, "All done!");
            logger.LogInfo($"*** Done in {DateTime.Now.Subtract(start).TotalSeconds} seconds ***");
        }
    }
}
