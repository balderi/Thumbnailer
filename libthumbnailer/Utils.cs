using System;
using System.Drawing;
using System.Drawing.Text;
using System.Text.Json;

namespace libthumbnailer
{
    public class Utils
    {
        public static string GetFps(string val)
        {
            var temp = val.Split('/');
            return (double.Parse(temp[0]) / double.Parse(temp[1])).ToString("N2");
        }

        public static int GetStringWidth(string text, Font font)
        {
            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            SizeF size = g.MeasureString(text, font);
            return (int)Math.Round(size.Width);
        }

        public static int GetStringHeight(string text, Font font)
        {
            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            SizeF size = g.MeasureString(text, font);
            return (int)Math.Round(size.Height);
        }

        public static string GetFileInfo(JsonElement format)
        {
            var size = format.TryGetProperty("size", out var Jsize) ? Jsize.GetString() : "N/A";
            var duration = format.TryGetProperty("duration", out var Jduration) ? Jduration.GetString() : "N/A";
            var bitRate = format.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Size: {size} bytes ({Converter.ToKiB(size)}B), duration: {Converter.ToHMS(double.Parse(duration))}, avg. bitrate: {Converter.ToKB(bitRate)}b/s";
        }

        public static string GetAudioInfo(JsonElement audioStream)
        {
            var codecName = audioStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString() : "N/A";
            var sampleRate = audioStream.TryGetProperty("sample_rate", out var JsampleRate) ? JsampleRate.GetString() : "N/A";
            var channels = audioStream.TryGetProperty("channels", out var Jchannels) ? Jchannels.GetInt32() : 0;
            var bitRate = audioStream.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Audio: {codecName}, {sampleRate} Hz, {channels} channels, {Converter.ToKB(bitRate)}b/s";
        }

        public static string GetVideoInfo(JsonElement videoStream)
        {
            var codecName = videoStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString() : "N/A";
            var width = videoStream.TryGetProperty("codec_name", out var Jwidth) ? Jwidth.GetString() : "N/A";
            var height = videoStream.TryGetProperty("codec_name", out var Jheight) ? Jheight.GetString() : "N/A";
            var frameRate = videoStream.TryGetProperty("avg_frame_rate", out var JframeRate) ? JframeRate.GetString() : "N/A";
            var bitRate = videoStream.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Video: {codecName}, {width}x{height}, {Utils.GetFps(frameRate)}, {Converter.ToKB(bitRate)}b/s";
        }

        public static FontFamily GetFontFamilyFromName(string name)
        {
            InstalledFontCollection fontCollection = new InstalledFontCollection();
            return Array.Find(fontCollection.Families, x => x.Name == name);
        }
    }
}
