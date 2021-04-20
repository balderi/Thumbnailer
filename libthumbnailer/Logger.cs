using System;
using System.IO;

namespace libthumbnailer
{
    public class Logger
    {
        readonly StreamWriter sw;
        readonly string logFile;

        public Logger()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            logFile = $"logs/{DateTime.Now:yyyyMMdd-HHmmss}.log";
            sw = new StreamWriter(logFile);
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
