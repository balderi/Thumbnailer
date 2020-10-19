using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace Thumbnailer
{
    class ContactSheet
    { 
        public string FilePath { get; }
        public double Duration { get; }
        public string FileInfo { get; }
        public string AudioInfo { get; }
        public string VideoInfo { get; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Width { get; set; }
        public int Height { get; private set; }
        public int Gap { get; set; }
        public List<Thumbnail> Thumbnails { get; }
        readonly Logger _logger;

        int aspectRatio = 1;

        public ContactSheet(string filePath, Logger logger)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _logger = logger;
            FilePath = filePath;

            var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = $"-show_streams -show_format -print_format json \"{filePath}\"",
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
            FileInfo = root.TryGetProperty("format", out format) ? GetFileInfo(format) : "Unknown file format";

            VideoInfo = TryGetIndex(root.GetProperty("streams"), "video", out int vindex) ?
                        GetVideoInfo(root.GetProperty("streams")[vindex]) : "Unknown video";

            AudioInfo = TryGetIndex(root.GetProperty("streams"), "audio", out int aindex) ?
                        GetAudioInfo(root.GetProperty("streams")[aindex]) : "Unknown audio";

            try
            {
                // If the file has some weird aspect ratio - like 2:1 - the thumbnails will be distorted
                // So we check for it and adjust accordingly elsewhere - otherwise the ratio is initialized as 1
                aspectRatio = int.Parse(root.GetProperty("streams")[vindex].GetProperty("sample_aspect_ratio").GetString().Split(':')[0]);
            }
            catch { }

            Thumbnails = new List<Thumbnail>();
            Height = 0;
        }

        bool TryGetIndex(JsonElement streams, string streamName, out int index)
        {
            int len = streams.GetArrayLength();

            for (int i = 0; i < len; i++)
            {
                if (streams[i].TryGetProperty("codec_type", out var codecName) && codecName.GetString() == streamName)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        string GetFileInfo(JsonElement format)
        {
            var size = format.TryGetProperty("size", out var Jsize) ? Jsize.GetString() : "N/A";
            var duration = format.TryGetProperty("duration", out var Jduration) ? Jduration.GetString() : "N/A";
            var bitRate = format.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Size: {size} bytes ({ConvertToKiB(size)}B), duration: {ConvertToHMS(double.Parse(duration))}, avg. bitrate: {ConvertToKB(bitRate)}b/s";
        }

        string GetAudioInfo(JsonElement audioStream)
        {
            var codecName = audioStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString() : "N/A";
            var sampleRate = audioStream.TryGetProperty("sample_rate", out var JsampleRate) ? JsampleRate.GetString() : "N/A";
            var channels = audioStream.TryGetProperty("channels", out var Jchannels) ? Jchannels.GetInt32() : 0;
            var bitRate = audioStream.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Audio: {codecName}, {sampleRate} Hz, {channels} channels, {ConvertToKB(bitRate)}b/s";
        }

        string GetVideoInfo(JsonElement videoStream)
        {
            var codecName = videoStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString() : "N/A";
            var width = videoStream.TryGetProperty("codec_name", out var Jwidth) ? Jwidth.GetString() : "N/A";
            var height = videoStream.TryGetProperty("codec_name", out var Jheight) ? Jheight.GetString() : "N/A";
            var frameRate = videoStream.TryGetProperty("avg_frame_rate", out var JframeRate) ? JframeRate.GetString() : "N/A";
            var bitRate = videoStream.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Video: {codecName}, {width}x{height}, {GetFps(frameRate)}, {ConvertToKB(bitRate)}b/s";
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
                    Arguments = "-i \"" + FilePath + "\" -vf fps=1/" + tween + " " + dir + "/img%05d.png",
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
                Thumbnail t = new Thumbnail(s, ++count * tween);
                Thumbnails.Add(t);
            }
        }

        int GetHeight()
        {
            int imgWidth = (Width - (Columns * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width;
            double th = Thumbnails[0].Image.Height / aspectRatio;
            double ratio = th / tw;
            int imgHeight = (int)Math.Round(ratio * imgWidth);
            return (imgHeight * Rows) + 2 + (Rows * Gap);
        }

        string GetFps(string val)
        {
            var temp = val.Split('/');
            return (double.Parse(temp[0]) / double.Parse(temp[1])).ToString("N2");
        }

        string ConvertToKiB(string value)
        {
            if(int.TryParse(value, out int result))
            {
                double temp = result;

                if (temp / 1073741824 > 1)
                {
                    return (temp / 1073741824).ToString("N2") + " Gi";
                }
                else if (temp / 1048576 > 1)
                {
                    return (temp / 1048576).ToString("N2") + " Mi";
                }
                else if (temp / 1024 > 1)
                {
                    return (temp / 1024).ToString("N2") + " Ki";
                }
                else
                {
                    return (temp).ToString();
                }
            }
            else
            {
                _logger.LogWarning($"Unable to parse value '{value}' - defaulting to 0");
                return "0 ";
            }
        }

        string ConvertToKB(string value)
        {
            if (int.TryParse(value, out int result))
            {
                double temp = result;

                if (temp / 1000000000 > 1)
                {
                    return (temp / 1000000000).ToString("N2") + " G";
                }
                else if (temp / 1000000 > 1)
                {
                    return (temp / 1000000).ToString("N2") + " M";
                }
                else if (temp / 1000 > 1)
                {
                    return (temp / 1000).ToString("N2") + " k";
                }
                else
                {
                    return (temp).ToString();
                }
            }
            else
            {
                _logger.LogWarning($"Unable to parse value '{value}' - defaulting to 0");
                return "0 ";
            }
        }

        string ConvertToHMS(double value)
        {
            TimeSpan t = TimeSpan.FromSeconds(value);
            return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }

        string PrintInfo()
        {
            return "File: " + new FileInfo(FilePath).Name + Environment.NewLine + FileInfo + Environment.NewLine + AudioInfo + Environment.NewLine + VideoInfo;
        }

        int GetStringWidth(string text, Font font)
        {
            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            SizeF size = g.MeasureString(text, font);
            return (int)Math.Round(size.Width);
        }

        int GetStringHeight(string text, Font font)
        {
            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            SizeF size = g.MeasureString(text, font);
            return (int)Math.Round(size.Height);
        }

        //public event EventHandler ThumbnailsGenerated;

        //protected void OnThumbnailsCreated(EventArgs e)
        //{
        //    ThumbnailsGenerated.Invoke(this, e);
        //}

        public event EventHandler SheetPrinted;

        protected void OnSheetPrinted(object sender, EventArgs e)
        {
            SheetPrinted.Invoke(this, e);
        }

        public bool PrintSheet(string filename, bool printInfo, FontFamily infoFont, int infoFontSize,
                               Color infoFontColor, bool printTime, FontFamily timeFont, int timeFontSize,
                               Color timeFontColor, Color timeShadowColor, Color backgroundColor)
        {
            GenerateThumbnails();
            //OnThumbnailsCreated(EventArgs.Empty);
            Height = GetHeight();

            int imgWidth = (Width - ((Columns - 1) * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width;
            double th = Thumbnails[0].Image.Height;
            double ratio = th / tw;
            int imgHeight = (int)Math.Round(ratio * imgWidth / aspectRatio);
            int infoHeight = 0;

            Font infoF = new Font(infoFont, infoFontSize);
            Font timeF = new Font(timeFont, timeFontSize);
            SolidBrush infoB = new SolidBrush(infoFontColor);
            SolidBrush timeB = new SolidBrush(timeFontColor);
            SolidBrush timeSB = new SolidBrush(timeShadowColor);

            if (printInfo)
            {
                infoHeight = GetStringHeight(PrintInfo(), infoF);
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
                                string tc = "";
                                try
                                {
                                    tc = ConvertToHMS(Thumbnails[idx].TimeCode);
                                }
                                catch (Exception e)
                                {
                                    tc = "?";
                                    _logger.LogWarning($"({FilePath}): {e.Message}");
                                }
                                int tcW = GetStringWidth(tc, timeF);
                                int tcH = GetStringHeight(tc, timeF);
                                canvas.DrawString(tc, timeF, timeSB, new PointF(x + imgWidth - tcW + 1, y + imgHeight - tcH + 1));
                                canvas.DrawString(tc, timeF, timeB, new PointF(x + imgWidth - tcW, y + imgHeight - tcH));
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
                    return false;
                }

                bitmap.Dispose();

                foreach(var t in Thumbnails)
                {
                    t.Dispose();
                }
            }
            OnSheetPrinted(this, EventArgs.Empty);
            return true;
        }

        public static event EventHandler SheetCreated;

        static void OnSheetCreated(EventArgs e)
        {
            SheetCreated?.Invoke(typeof(ContactSheet), e);
        }

        public static List<ContactSheet> BuildSheets(string[] files, Logger logger)
        {
            var retval = new List<ContactSheet>();

            foreach(string file in files)
            {
                try
                {
                    ContactSheet cs = new ContactSheet(file, logger);
                    retval.Add(cs);
                    OnSheetCreated(EventArgs.Empty);
                }
                catch (Exception e)
                {
                    logger.LogError($"Exception during sheet-building of file {file}: {e.Message}");
                    //throw e;
                    continue;
                }
            }

            return retval;
        }
    }
}
