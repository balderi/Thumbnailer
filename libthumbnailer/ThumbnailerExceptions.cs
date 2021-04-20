using System;

namespace libthumbnailer
{
    class FfmpegException : Exception
    {
        public override string Message => "ffmpeg failed to start. Is it installed and added to PATH?";
    }
    class FfprobeException : Exception
    {
        public override string Message => "ffprobe failed to start. Is it installed and added to PATH?";
    }
}
