using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

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

        public ContactSheet(string filePath, Logger logger)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            _logger = logger;
            FilePath = filePath;
            Duration = GetDuration();
            FileInfo = GetFileInfo();
            AudioInfo = GetAudioInfo();
            VideoInfo = GetVideoInfo();
            Thumbnails = new List<Thumbnail>();
            Height = 0;
        }

        void GenerateThumbnails()
        {
            double tween = Duration / (Rows * Columns);

            //for (int i = 0; i < (Rows * Columns); i++)
            //{
            //    var start = DateTime.Now;
            //    _logger.LogInfo($"Current frame = {(int)(i * tween * _vr.FrameRate.ToDouble())}");
            //    Thumbnail t = new Thumbnail(_vr.ReadVideoFrame((int)(i * tween * _vr.FrameRate.ToDouble())), i * tween);
            //    Thumbnails.Add(t);
            //    _logger.LogInfo($"Time for frame: {DateTime.Now.Subtract(start).TotalSeconds} seconds");
            //}

            string dir = "temp/temp_" + (FilePath.GetHashCode() + DateTime.Now.Millisecond);
            Directory.CreateDirectory(dir);
            _logger.LogInfo($"Generating thumbnails for file {FilePath}, in directory {dir}");

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-i \"" + FilePath + "\" -vf fps=1/" + tween + " " + dir + "/img%05d.bmp",
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
                //_logger.LogInfo($"Current frame = {count * tween * 29.97}");
                Thumbnails.Add(t);
            }
        }

        double GetDuration()
        {
            //return Math.Round(_vr.FrameCount / _vr.FrameRate.ToDouble());
            _logger.LogInfo($"Getting duration for file {FilePath}...");
            var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = "-v quiet -print_format compact=print_section=0:nokey=1:escape=csv -show_entries format=duration \"" + FilePath + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            string output;
            try
            {
                probe.Start();
                output = probe.StandardOutput.ReadToEnd().Replace(Environment.NewLine, "").Trim();
                probe.WaitForExit();
                probe.Dispose();
            }
            catch
            {
                _logger.LogError($"ffprobe failed to start.");
                throw new FfprobeException();
            }

            if (!double.TryParse(output, out double retval))
            {
                _logger.LogError($"Invalid duration for file {FilePath}...");
                throw new ArgumentException("Invalid duration");
            }
            else
            {
                return Math.Round(retval);
            }
        }

        string GetFileInfo()
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = "-v quiet -print_format compact=print_section=0:nokey=1:escape=csv -show_entries format=size,duration,bit_rate \"" + FilePath + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            string[] output;
            try
            {
                probe.Start();
                output = probe.StandardOutput.ReadToEnd().Replace(Environment.NewLine, "").Trim().Split('|');
                probe.WaitForExit();
                probe.Dispose();
            }
            catch
            {
                _logger.LogError($"ffprobe failed to start.");
                throw new FfprobeException();
            }

            try
            {
                return $"Size: {output[1]} bytes ({ConvertToKiB(output[1])}B), duration: {ConvertToHMS(double.Parse(output[0]))}, avg. bitrate: {ConvertToKB(output[2])}b/s";
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return "unknown";
            }
        }

        string GetAudioInfo()
        {
            var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = "-v quiet -print_format compact=print_section=0:nokey=1:escape=csv -select_streams a:0 -show_entries stream=codec_name,sample_rate,channels,bit_rate \"" + FilePath + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            string[] output;
            try
            {
                probe.Start();
                output = probe.StandardOutput.ReadToEnd().Replace(Environment.NewLine, "").Trim().Split('|');
                probe.WaitForExit();
                probe.Dispose();
            }
            catch
            {
                _logger.LogError($"ffprobe failed to start.");
                throw new FfprobeException();
            }

            try
            {
                return $"Audio: {output[0]}, {output[1]} Hz, {output[2]} channels, {ConvertToKB(output[3])}b/s";
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Getting audio info on file {FilePath} faled with exception: {e.Message}");
                return "Audio: unknown";
            }
        }

        string GetVideoInfo()
        {
            var probe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffprobe",
                    Arguments = "-v quiet -print_format compact=print_section=0:nokey=1:escape=csv -select_streams v:0 -show_entries stream=codec_name,width,height,r_frame_rate,bit_rate \"" + FilePath + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            string[] output;
            try
            {
                probe.Start();
                output = probe.StandardOutput.ReadToEnd().Replace(Environment.NewLine, "").Trim().Split('|');
                probe.WaitForExit();
                probe.Dispose();
            }
            catch
            {
                _logger.LogError($"ffprobe failed to start.");
                throw new FfprobeException();
            }

            try
            {
                return $"Video: {output[0]}, {output[1]}x{output[2]}, {GetFps(output[3])}, {ConvertToKB(output[4])}b/s";
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Getting video info on file {FilePath} faled with exception: {e.Message}");
                return "Video: unknown";
            }
        }

        int GetHeight()
        {
            int imgWidth = (Width - (Columns * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width;
            double th = Thumbnails[0].Image.Height;
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

        public bool PrintSheet(string filename, bool printInfo, FontFamily infoFont, int infoFontSize,
                               Color infoFontColor, bool printTime, FontFamily timeFont, int timeFontSize,
                               Color timeFontColor, Color timeShadowColor, Color backgroundColor)
        {
            GenerateThumbnails();
            Height = GetHeight();

            int imgWidth = (Width - ((Columns - 1) * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width;
            double th = Thumbnails[0].Image.Height;
            double ratio = th / tw;
            int imgHeight = (int)Math.Round(ratio * imgWidth);
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
                    _logger.LogInfo($"Successfully saved file {filename}.png");
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
            return true;
        }
    }
}
