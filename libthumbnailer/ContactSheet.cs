using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Globalization;
using System.Threading.Tasks;

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

        readonly Logger _logger;
        int _aspectRatio = 1;

        public static event EventHandler<string> SheetCreated;
        public event EventHandler<string> SheetPrinted;
        public static event EventHandler<string> AllSheetsPrinted;

        public ContactSheet(string filePath, Logger logger)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _logger = logger;
            FilePath = filePath;
            GetInfo();
            Thumbnails = new List<Thumbnail>();
            Height = 0;
        }

        void GetInfo()
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
            JsonElement root = doc.RootElement;

            Duration = root.TryGetProperty("format", out var format) ? double.Parse(format.GetProperty("duration").GetString()) : -1;
            FileInfo = root.TryGetProperty("format", out format) ? Utils.GetFileInfo(format) : "Unknown file format";

            VideoInfo = TryGetIndex(root.GetProperty("streams"), "video", out int vindex) ?
                        Utils.GetVideoInfo(root.GetProperty("streams")[vindex]) : "Unknown video";

            AudioInfo = TryGetIndex(root.GetProperty("streams"), "audio", out int aindex) ?
                        Utils.GetAudioInfo(root.GetProperty("streams")[aindex]) : "Unknown audio";

            // If the file has some weird aspect ratio - like 2:1 - the thumbnails will be distorted
            // So we check for it and adjust accordingly elsewhere - otherwise the ratio is initialized as 1
            if(vindex > 0)
            {
                if (root.GetProperty("streams")[vindex].TryGetProperty("sample_aspect_ratio", out var aspect))
                {
                    int w = int.Parse(aspect.GetString().Split(':')[0]);
                    int h = int.Parse(aspect.GetString().Split(':')[1]);
                    if (w > h)
                        _aspectRatio = w / h;
                }
            }
        }

        bool TryGetIndex(JsonElement streams, string streamName, out int index)
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

        void GenerateThumbnails()
        {
            double tween = Duration / (Rows * Columns);

            string dir = "temp/temp_" + (FilePath.GetHashCode() + DateTime.Now.Millisecond);
            Directory.CreateDirectory(dir);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{FilePath}\" -vf fps=1/{tween} {dir}/img%05d.png",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            //var StartInfo = new ProcessStartInfo
            //{
            //    FileName = "ffmpeg",
            //    Arguments = $"-i \"{FilePath}\" -vf fps=1/{tween} {dir}/img%05d.png",
            //    UseShellExecute = false,
            //    RedirectStandardOutput = true,
            //    RedirectStandardError = true,
            //    CreateNoWindow = true
            //};

            //var proc = ProcessAsync.RunAsync(StartInfo, 10000);
            //await proc;
            //_logger.LogInfo(proc.Result.StdErr.Replace("\n", Environment.NewLine));
            //_logger.LogInfo(proc.Result.StdOut.Replace("\n", Environment.NewLine));

            try
            {
                proc.Start();
                proc.WaitForExit();
                proc.Dispose();
            }
            catch
            {
                _logger.LogError($"ffmpeg failed to start.");
                throw new FfmpegException();
            }

            int count = 0;
            foreach (string s in Directory.GetFiles(dir))
            {
                Thumbnails.Add(ThumbnailFactory.CreateThumbnail(s, ++count * tween));
            }
        }

        string PrintInfo()
        {
            return "File: " + new FileInfo(FilePath).Name + 
                            Environment.NewLine + FileInfo + 
                            Environment.NewLine + AudioInfo + 
                            Environment.NewLine + VideoInfo;
        }

        public bool PrintSheet(string filename, bool printInfo, FontFamily infoFont, int infoFontSize,
                               Color infoFontColor, bool printTime, FontFamily timeFont, int timeFontSize,
                               Color timeFontColor, Color timeShadowColor, Color backgroundColor)
        {
            GenerateThumbnails();

            int imgWidth = (Width - ((Columns - 1) * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width;
            double th = Thumbnails[0].Image.Height;
            double ratio = th / tw;
            int imgHeight = (int)Math.Round(ratio * imgWidth / _aspectRatio);
            Height = (imgHeight * Rows) + 2 + (Rows * Gap);
            int infoHeight = 0;

            Font infoF = FontFactory.CreateFont(infoFont, infoFontSize);
            Font timeF = FontFactory.CreateFont(timeFont, timeFontSize);
            SolidBrush infoB = BrushFactory.CreateBrush(infoFontColor);
            SolidBrush timeB = BrushFactory.CreateBrush(timeFontColor);
            SolidBrush timeSB = BrushFactory.CreateBrush(timeShadowColor);

            if (printInfo)
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
                    canvas.Clear(backgroundColor);
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
                            if (printTime)
                            {
                                string time = "";
                                try
                                {
                                    time = Converter.ToHMS(Thumbnails[idx].TimeCode);
                                }
                                catch (Exception e)
                                {
                                    time = "?";
                                    _logger.LogWarning($"({FilePath}): {e.Message}");
                                }
                                int timeWidth = Utils.GetStringWidth(time, timeF);
                                int timeHeight = Utils.GetStringHeight(time, timeF);
                                canvas.DrawString(time, timeF, timeSB, new PointF(x + imgWidth - timeWidth + 1, y + imgHeight - timeHeight + 1));
                                canvas.DrawString(time, timeF, timeB, new PointF(x + imgWidth - timeWidth, y + imgHeight - timeHeight));
                            }
                            idx++;
                        }
                    }

                    if (printInfo)
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

                bitmap.Dispose();

                foreach (var t in Thumbnails)
                {
                    t.Dispose();
                }
            }
            SheetPrinted?.Invoke(this, FilePath);
            return true;
        }

        public static List<ContactSheet> BuildSheets(string[] files, Logger logger)
        {
            var retval = new List<ContactSheet>();

            foreach (string file in files)
            {
                try
                {
                    //ContactSheet cs = new ContactSheet(file, logger);
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

        public static async Task PrintSheets(List<ContactSheet> sheets, Logger logger, string outputPath = null)
        {
            var start = DateTime.Now;
            var results = new Task<bool>[sheets.Count];
            int i = 0;

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

                var t = new Task<bool>(() =>
                {
                    return cs.PrintSheet(filePath, Config.CurrentConfig.PrintInfo, 
                                                     Utils.GetFontFamilyFromName(Config.CurrentConfig.InfoFont), 
                                                     Config.CurrentConfig.InfoFontSize, 
                                                     Color.FromArgb(Config.CurrentConfig.InfoColor), 
                                                     Config.CurrentConfig.PrintTime, 
                                                     Utils.GetFontFamilyFromName(Config.CurrentConfig.TimeFont), 
                                                     Config.CurrentConfig.TimeFontSize, 
                                                     Color.FromArgb(Config.CurrentConfig.TimeColor), 
                                                     Color.FromArgb(Config.CurrentConfig.ShadowColor), 
                                                     Color.FromArgb(Config.CurrentConfig.BackgroundColor));
                });
                results[i] = t;
                t.Start();
                i++;
            }

            await Task.WhenAll(results);

            AllSheetsPrinted?.Invoke(null, "All done!");
            logger.LogInfo($"*** Done in {DateTime.Now.Subtract(start).TotalSeconds} seconds ***");
        }
    }
}
