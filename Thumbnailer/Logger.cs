using System;
using System.IO;

namespace Thumbnailer
{
    public class Logger
    {
        readonly StreamWriter sw;
        public Logger()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            sw = new StreamWriter($"logs/{DateTime.Now:ddMMyyyyHHmmss}.log");
            sw.WriteLine($"--- BEGIN LOG - {DateTime.Now} ---");
        }

        public void Log(string message)
        {
            sw.WriteLine(message);
        }

        public void LogError(string message)
        {
            Log($"[{DateTime.Now}] ERROR: {message}");
        }

        public void LogWarning(string message)
        {
            Log($"[{DateTime.Now}] WARNING: {message}");
        }

        public void LogInfo(string message)
        {
            Log($"[{DateTime.Now}] INFO: {message}");
        }

        public void Close()
        {
            sw.Close();
        }
    }
}
