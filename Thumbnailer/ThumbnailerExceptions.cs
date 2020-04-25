using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thumbnailer
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
