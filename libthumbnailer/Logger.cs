using System;
using System.IO;

namespace libthumbnailer
{
    public class Logger
    {
        StreamWriter sw;
        readonly string logFile;

        public Logger()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            logFile = $"logs/{DateTime.Now:yyyyMMdd-HHmmss}.log";
            sw = new StreamWriter(logFile);
            sw.WriteLine($"--- BEGIN LOG - {DateTime.Now} ---");
            sw.Close();
        }

        public void Log(string message)
        {
            sw = new StreamWriter(logFile);
            sw.WriteLine(message);
            sw.Close();
        }

        public void LogError(string message)
        {
            //sw = new StreamWriter(logFile);
            Log($"[{DateTime.Now}] ERROR: {message}");
            //sw.Close();
        }

        public void LogWarning(string message)
        {
            //sw = new StreamWriter(logFile);
            Log($"[{DateTime.Now}] WARNING: {message}");
            //sw.Close();
        }

        public void LogInfo(string message)
        {
            //sw = new StreamWriter(logFile);
            Log($"[{DateTime.Now}] INFO: {message}");
            //sw.Close();
        }

        public void Close()
        {
            sw.Close();
        }
    }
}
