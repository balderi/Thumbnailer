using System;
using System.Drawing;
using System.Drawing.Text;
using System.Text.Json;

namespace libthumbnailer
{
    public class Utils
    {
        /// <summary>
        /// Calculates the framerate of the file.
        /// </summary>
        /// <param name="val">The <see cref="string"/> to parse.</param>
        /// <returns>The framerate with two decimal places as <see cref="string"/>.</returns>
        public static string GetFps(string val)
        {
            var temp = val.Split('/');
            return (double.Parse(temp[0]) / double.Parse(temp[1])).ToString("N2");
        }

        /// <summary>
        /// Get the pixel width of a text string.
        /// </summary>
        /// <param name="text">The text string to measure.</param>
        /// <param name="font">The font used to print the text.</param>
        /// <returns>The width of the text string in pixels.</returns>
        public static int GetStringWidth(string text, Font font)
        {
            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            SizeF size = g.MeasureString(text, font);
            return (int)Math.Round(size.Width);
        }

        /// <summary>
        /// Get the pixel height of a text string.
        /// </summary>
        /// <param name="text">The text string to measure.</param>
        /// <param name="font">The font used to print the text.</param>
        /// <returns>The height of the text string in pixels.</returns>
        public static int GetStringHeight(string text, Font font)
        {
            Graphics g = Graphics.FromImage(new Bitmap(1, 1));
            SizeF size = g.MeasureString(text, font);
            return (int)Math.Round(size.Height);
        }

        /// <summary>
        /// Gets the basic info about the file from a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="format">The <see cref="JsonElement"/> to parse.</param>
        /// <returns>A formatted <see cref="string"/> containing basic file info.</returns>
        public static string GetFileInfo(JsonElement format)
        {
            var size = format.TryGetProperty("size", out var Jsize) ? Jsize.GetString() : "N/A";
            var duration = format.TryGetProperty("duration", out var Jduration) ? Jduration.GetString() : "N/A";
            var bitRate = format.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Size: {size} bytes ({Converter.ToKiB(size)}B), duration: {Converter.ToHMS(double.Parse(duration))}, avg. bitrate: {Converter.ToKB(bitRate)}b/s";
        }

        /// <summary>
        /// Gets audio info about the file from a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="audioStream">The <see cref="JsonElement"/> to parse.</param>
        /// <returns>A formatted <see cref="string"/> containing audio info.</returns>
        public static string GetAudioInfo(JsonElement audioStream)
        {
            var codecName = audioStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString() : "N/A";
            var sampleRate = audioStream.TryGetProperty("sample_rate", out var JsampleRate) ? JsampleRate.GetString() : "N/A";
            var channels = audioStream.TryGetProperty("channels", out var Jchannels) ? Jchannels.GetInt32() : 0;
            var bitRate = audioStream.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Audio: {codecName}, {sampleRate} Hz, {channels} channels, {Converter.ToKB(bitRate)}b/s";
        }

        /// <summary>
        /// Gets video info about the file from a <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="audioStream">The <see cref="JsonElement"/> to parse.</param>
        /// <returns>A formatted <see cref="string"/> containing video info.</returns>
        public static string GetVideoInfo(JsonElement videoStream)
        {
            var codecName = videoStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString() : "N/A";
            var width = videoStream.TryGetProperty("width", out var Jwidth) ? Jwidth.GetInt32().ToString() : "N/A";
            var height = videoStream.TryGetProperty("height", out var Jheight) ? Jheight.GetInt32().ToString() : "N/A";
            var frameRate = videoStream.TryGetProperty("avg_frame_rate", out var JframeRate) ? JframeRate.GetString() : "N/A";
            var bitRate = videoStream.TryGetProperty("bit_rate", out var JbitRate) ? JbitRate.GetString() : "N/A";

            return $"Video: {codecName}, {width}x{height}, {GetFps(frameRate)}, {Converter.ToKB(bitRate)}b/s";
        }

        /// <summary>
        /// Gets the specified <see cref="FontFamily"/>.
        /// </summary>
        /// <param name="name">Name of the font.</param>
        /// <returns>The <see cref="FontFamily"/> if it is installed; otherwise the default value for <see cref="FontFamily"/>.</returns>
        public static FontFamily GetFontFamilyFromName(string name)
        {
            InstalledFontCollection fontCollection = new InstalledFontCollection();
            return Array.Find(fontCollection.Families, x => x.Name == name);
        }
    }
}
