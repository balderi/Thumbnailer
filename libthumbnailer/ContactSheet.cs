using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Serilog;
using SixLabors.ImageSharp.Processing;

namespace libthumbnailer
{
    public class ContactSheet
    {
        public string FilePath { get; private set; } = string.Empty;
        public double Duration { get; private set; }
        public string FileInfo { get; private set; } = string.Empty;
        public string AudioInfo { get; private set; } = string.Empty;
        public string VideoInfo { get; private set; } = string.Empty;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Width { get; set; }
        public int Height { get; private set; }
        public int Gap { get; set; }
        public List<Thumbnail> Thumbnails { get; }

        private readonly int _aspectRatio = 1;
        private readonly Config _config;
        private readonly ILogger _logger;
        private string _thumbDir = string.Empty;

        public static event EventHandler<string>? SheetCreated;
        public event EventHandler<string>? SheetPrinted;
        //public static event EventHandler<string>? AllSheetsPrinted;

        public ContactSheet(string path, Config config, ILogger logger)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            FilePath = path;
            _config = config;
            _logger = logger;

            if (config is null || config.Rows == 0 || config.Columns == 0)
            {
                _logger.WithClassAndMethodNames<ContactSheet>().Warning("Config is invalid - loading default config");
                config = Config.Load("default.json");
            }

            var info = GetRootInfo();
            Duration = GetDuration(info);
            FileInfo = GetFileInfo(info);
            VideoInfo = GetVideoInfo(info);
            AudioInfo = GetAudioInfo(info);

            Rows = _config.Rows;
            Columns = _config.Columns;
            Width = _config.Width;
            Gap = _config.Gap;
            _aspectRatio = GetAspectRatio(info);
            _logger.WithClassAndMethodNames<ContactSheet>().Information("Got aspect ratio of {ratio}", _aspectRatio);

            Thumbnails = [];
            Height = 0;

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Initialized new contact sheet with W:{w}, H:{h}, G:{g}", Width, Height, Gap);
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

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Starting ffprobe");

            var sw = new Stopwatch();

            sw.Start();

            probe.Start();
            output = probe.StandardOutput.ReadToEnd().Trim();
            probe.WaitForExit();
            probe.Dispose();

            sw.Stop();

            _logger.WithClassAndMethodNames<ContactSheet>().Information("ffprobe finished in {time}ms", sw.ElapsedMilliseconds);

            JsonDocument doc = JsonDocument.Parse(output);
            return doc.RootElement;
        }

        private double GetDuration(JsonElement root)
        {
            var duration = root.TryGetProperty("format", out var format) ? double.Parse(format.GetProperty("duration").GetString()!) : -1;
            _logger.WithClassAndMethodNames<ContactSheet>().Information("Got duration of {duration}", duration);
            return duration;
        }

        private static string GetFileInfo(JsonElement root)
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

        private static string GetVideoInfo(JsonElement root)
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

        private int GetAspectRatio(JsonElement root)
        {
            try
            {
                TryGetIndex(root.GetProperty("streams"), "video", out int vindex);
                var video = root.GetProperty("streams")[vindex];
                var aspect = video.TryGetProperty("sample_aspect_ratio", out var ratio);
                if (aspect)
                {
                    var parts = ratio.GetString()!.Split(':');
                    return int.Parse(parts[0]) / int.Parse(parts[1]);
                }
                else
                {
                    _logger.WithClassAndMethodNames<ContactSheet>().Warning("Unable to get sample_aspect_ratio property. Setting aspect ratio to 1");
                    return 1;
                }
            }
            catch (Exception e)
            {
                _logger.WithClassAndMethodNames<ContactSheet>().Error("Unable to get sample_aspect_ratio property. Setting aspect ratio to 1. Exception caught: {ex}", e);
                return 1;
            }
        }

        private static string GetAudioInfo(JsonElement root)
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

        private static bool TryGetIndex(JsonElement streams, string streamName, out int index)
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

            _logger.WithClassAndMethodNames<ContactSheet>().Information("tween is {tween} ({duration} / {rows}*{cols})", tween, Duration, Rows, Columns);

            var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{FilePath}\" -vf fps=1/{tween} {_thumbDir}/img%05d.jpeg",
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Starting ffmpeg with arguments {args}", proc.StartInfo.Arguments);

            var sw = new Stopwatch();

            sw.Start();

            proc.Start();
            var output = proc.StandardError.ReadToEnd().Trim();
            proc.WaitForExit();
            proc.Dispose();

            sw.Stop();

            _logger.WithClassAndMethodNames<ContactSheet>().Information("ffmpeg finished in {time}ms", sw.ElapsedMilliseconds);

            int count = 0;

            var holder = Directory.GetFiles(_thumbDir);

            if(holder.Length == 0)
            {
                _logger.WithClassAndMethodNames<ContactSheet>().Error("ffmpeg generated no images - Output:\n{out}", output);
                throw new InvalidOperationException("No images were generated by ffmpeg");
            }

            // Sort the list of screenshots lexicographically, because linux reads files weird, apparently...
            var sorted = from s in holder orderby s select s;

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Generated {n} pictures", holder.Length);

            foreach (string s in sorted)
            {
                Thumbnails.Add(ThumbnailFactory.CreateThumbnail(s, ++count * tween));

                _logger.WithClassAndMethodNames<ContactSheet>().Information("Creating thumbnail {n} of {total}", count, holder.Length);
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
        public bool PrintSheet(bool overwrite)
        {
            _logger.WithClassAndMethodNames<ContactSheet>().Information("Printing contact sheet");

            if (!overwrite && File.Exists(FilePath + ".png"))
            {
                return true;
            }

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Generating thumbnails");
            GenerateThumbnails();

            int thumbWidth = (Width - ((Columns - 1) * Gap) - 4) / Columns;
            double tw = Thumbnails[0].Image.Width * _aspectRatio;
            double th = Thumbnails[0].Image.Height;
            double ratio = th / tw;
            int thumbHeight = (int)Math.Round(ratio * thumbWidth);
            Height = (thumbHeight * Rows) + 2 + (Rows * Gap);
            int infoHeight = 0;

            Font infoF = new(Utils.GetFontFamilyFromName(_config.InfoFont), _config.InfoFontSize);
            Font timeF = new(Utils.GetFontFamilyFromName(_config.TimeFont), _config.TimeFontSize);
            SolidBrush infoB = BrushFactory.CreateBrush(Color.FromPixel<Rgba32>(Rgba32.ParseHex(_config.InfoFontColor)));
            SolidBrush timeB = BrushFactory.CreateBrush(Color.FromPixel<Rgba32>(Rgba32.ParseHex(_config.TimeFontColor)));
            SolidBrush timeSB = BrushFactory.CreateBrush(Color.FromPixel<Rgba32>(Rgba32.ParseHex(_config.ShadowColor)));

            if (_config.PrintInfo)
            {
                infoHeight = Utils.GetStringHeight(PrintInfo(), infoF) + 5;
                Height += infoHeight;
            }

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Printing contact sheet");

            using var image = new Image<Rgba32>(Width, Height);
            image.Mutate(i => i.Fill(Color.Black));
            int idx = 0;

            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    int x = 2 + (j * (thumbWidth + Gap));
                    int y = infoHeight + (i * (thumbHeight + Gap));

                    _logger.WithClassAndMethodNames<ContactSheet>().Information("Drawing image {idx} at ({x},{y})", idx, x, y);

                    var tn = Thumbnails[idx].Image;
                    tn.Mutate(t => t.Resize(thumbWidth, thumbHeight));

                    image.Mutate(i => i.DrawImage(tn, new Point(x, y), 1f));

                    if (_config.PrintTime)
                    {
                        string time = Converter.ToHMS(Thumbnails[idx].TimeCode);

                        int timeWidth = Utils.GetStringWidth(time, timeF);
                        int timeHeight = Utils.GetStringHeight(time, timeF);
                        image.Mutate(i => i.DrawText(time, timeF, timeSB, new PointF(x + thumbWidth - timeWidth + 1, y + thumbHeight - timeHeight + 1)));
                        image.Mutate(i => i.DrawText(time, timeF, timeB, new PointF(x + thumbWidth - timeWidth, y + thumbHeight - timeHeight)));
                    }
                    idx++;
                }
            }

            if (_config.PrintInfo)
                image.Mutate(i => i.DrawText(PrintInfo(), infoF, infoB, new PointF(2, 2)));

            try
            {
                image.SaveAsPng(FilePath + ".png");
            }
            catch (Exception e)
            {
                _logger.WithClassAndMethodNames<ContactSheet>().Fatal(e.Message);
            }

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Cleaning up");

            foreach (var t in Thumbnails)
            {
                t.Dispose();
                try
                {
                    File.Delete(t.Path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    _logger.WithClassAndMethodNames<ContactSheet>().Error(ex.Message);
                }
            }
            try
            {
                Directory.Delete(_thumbDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                _logger.WithClassAndMethodNames<ContactSheet>().Error(ex.Message);
            }

            _logger.WithClassAndMethodNames<ContactSheet>().Information("Contact sheet printed successfully to {path}", FilePath + ".png");
            SheetPrinted?.Invoke(this, FilePath);
            return true;
        }

        public List<ContactSheet> BuildSheets(IEnumerable<string> files)
        {
            var retval = new List<ContactSheet>();

            foreach (string file in files)
            {
                retval.Add(ContactSheetFactory.CreateContactSheet(file, _config, _logger));
                SheetCreated?.Invoke(null, file);
            }

            return retval;
        }
    }
}
