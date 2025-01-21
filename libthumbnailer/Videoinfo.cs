using System.Diagnostics;
using System.Text.Json;

namespace libthumbnailer
{
    public static class Videoinfo
    {
        public static Dictionary<string, string> GetVideoInfo(string filePath)
        {
            var retval = new Dictionary<string, string>();

            JsonElement root = new();
            try
            {
                root = GetRootInfo(filePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            try
            {
                if (TryGetIndex(root.GetProperty("streams"), "video", out int vindex))
                {
                    var videoStream = root.GetProperty("streams")[vindex];

                    retval.Add("Width", videoStream.TryGetProperty("width", out var Jwidth) ? Jwidth.GetInt32().ToString()! : "N/A");
                    retval.Add("Height", videoStream.TryGetProperty("height", out var Jheight) ? Jheight.GetInt32().ToString()! : "N/A");
                    retval.Add("Codec", videoStream.TryGetProperty("codec_name", out var JcodecName) ? JcodecName.GetString()! : "N/A");
                    retval.Add("FrameRate", videoStream.TryGetProperty("avg_frame_rate", out var Jfps) ? Jfps.GetString()! : "N/A");
                }
            }
            catch { }
            try
            {
                if (TryGetIndex(root.GetProperty("streams"), "audio", out int aindex))
                {
                    var audioStream = root.GetProperty("streams")[aindex];

                    retval.Add("AudioCodec", audioStream.TryGetProperty("codec_name", out var Jacodec) ? Jacodec.GetString()! : "N/A");
                    retval.Add("SampleRate", audioStream.TryGetProperty("sample_rate", out var Jrate) ? Jrate.GetString()! : "N/A");
                }
            }
            catch { }
            try
            {
                if (root.TryGetProperty("format", out var format))
                {
                    retval.Add("Duration", format.TryGetProperty("duration", out var Jduration) ? Jduration.GetString()! : "N/A");
                    retval.Add("Size", format.TryGetProperty("size", out var Jsize) ? Jsize.GetString()! : "N/A");
                    retval.Add("Format", format.TryGetProperty("format_name", out var Jformat) ? Jformat.GetString()! : "N/A");
                    retval.Add("BitRate", format.TryGetProperty("bit_rate", out var Jbitrate) ? Jbitrate.GetString()! : "N/A");
                }
            }
            catch { }

            return retval;
        }

        private static JsonElement GetRootInfo(string filePath)
        {
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
            probe.Start();
            output = probe.StandardOutput.ReadToEnd().Trim();
            probe.WaitForExit();
            probe.Dispose();

            JsonDocument doc = JsonDocument.Parse(output);
            return doc.RootElement;
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
    }
}
