using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
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

        public ContactSheet(string filePath, Logger logger)
        {
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

            try
            {
                // If the file has some weird aspect ratio - like 2:1 - the thumbnails will be distorted
                // So we check for it and adjust accordingly elsewhere - otherwise the ratio is initialized as 1
                _aspectRatio = int.Parse(root.GetProperty("streams")[vindex].GetProperty("sample_aspect_ratio").GetString().Split(':')[0]);
            }
            catch { }
        }

        bool TryGetIndex(JsonElement streams, string streamName, out int index)
        {
            int len = streams.GetArrayLength();

            for (int i = 0; i < len; i++)
            {
                if (streams[i].TryGetProperty("codec_name", out var codecName) && codecName.GetString() == streamName)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
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

        public Task<bool> PrintSheet(string filename, bool printInfo, FontFamily infoFont, int infoFontSize,
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
                    if (printInfo)
                        canvas.DrawString(PrintInfo(), infoF, infoB, new PointF(2, 2));

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
                    canvas.Save();
                }
                try
                {
                    bitmap.Save(filename + ".png", ImageFormat.Png);
                }
                catch (Exception e)
                {
                    _logger.LogError($"Unable to save file {filename}: {e.Message}");
                    return Task.FromResult(false);
                }

                bitmap.Dispose();

                foreach (var t in Thumbnails)
                {
                    t.Dispose();
                }
            }
            SheetBuiltEvent?.Invoke(new SheetPrintedEventArgs(FilePath));
            return Task.FromResult(true);
        }

        public delegate void SheetPrintedEventHandler(SheetPrintedEventArgs e);

        public event SheetPrintedEventHandler SheetBuiltEvent;
    }

    public class SheetPrintedEventArgs
    {
        public string FilePath { get; set; }
        public SheetPrintedEventArgs(string filePath) { FilePath = filePath; }
    }

}
