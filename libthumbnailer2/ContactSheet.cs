using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FFMpegCore;

namespace libthumbnailer2
{
    public class ContactSheet
    {
        public string FilePath { get; private set; }
        public TimeSpan Duration { get; private set; }
        public string FileInfo { get; private set; }
        public string AudioInfo { get; private set; }
        public string VideoInfo { get; private set; }
        public int Rows { get; set; }
        public int Columns { get; set; }
        public int Width { get; set; }
        public int Height { get; private set; }
        public int Gap { get; set; }

        private IMediaAnalysis _media;

        public ContactSheet(string filePath)
        {
            FilePath = filePath;
        }

        private async Task<IMediaAnalysis> AnalyzeMedia()
        {
            return await FFProbe.AnalyseAsync(FilePath);
        }

        private async Task<bool> GenerateMetaData()
        {
            try
            {
                var media = await AnalyzeMedia();
                var info = new FileInfo(FilePath);

                Duration = media.Duration;

                StringBuilder builder = new();
                builder.AppendLine("File: " + Path.GetFileName(FilePath));
                builder.Append($"Size: {info.Length} ({Converter.ToKiB(info.Length)}), duration: {Duration.Hours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}, avg. bitrate: {Converter.ToKB(media.PrimaryVideoStream.BitRate)}/s");
                FileInfo = builder.ToString();

                AudioInfo = $"Audio: {media.PrimaryAudioStream.CodecName}, {media.PrimaryAudioStream.SampleRateHz} Hz, {media.PrimaryAudioStream.Channels} channels, {Converter.ToKB(media.PrimaryAudioStream.BitRate)}/s";
                VideoInfo = $"Video: {media.PrimaryVideoStream.CodecName}, {media.PrimaryVideoStream.Width}x{media.PrimaryVideoStream.Height}, {media.PrimaryVideoStream.FrameRate:N2}, {Converter.ToKB(media.PrimaryVideoStream.BitRate)}/s";
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<ContactSheet> PrintSheet()
        {
            if(!await GenerateMetaData())
            {
                // do something
                return new ContactSheet(FilePath);
            }
            return new ContactSheet(FilePath);
        }
    }
}
