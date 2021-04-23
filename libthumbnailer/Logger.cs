using System;
using System.IO;

namespace libthumbnailer
{
    /// <summary>
    /// A simple logger for writing basic log files.
    /// </summary>
    /// <remarks>Remember to call <see cref="Close"/> when done.</remarks>
    public class Logger
    {
        readonly StreamWriter sw;
        readonly string logFile;

        /// <summary>
        /// Initialize a new <see cref="Logger"/> instance.
        /// </summary>
        /// <remarks>
        /// <para>The corresponding file is named by the time this instance is created in the format <c>yyyyMMdd-HHmmss</c>.</para>
        /// <para>Remember to call <see cref="Close"/> when done.</para>
        /// </remarks>
        /// <remarks></remarks>
        public Logger()
        {
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            logFile = $"logs/{DateTime.Now:yyyyMMdd-HHmmss}.log";
            sw = new StreamWriter(logFile);
            sw.WriteLine($"--- BEGIN LOG - {DateTime.Now} ---");
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>Prepends date and time.</remarks>
        public void Log(string message)
        {
            sw.WriteLine($"[{DateTime.Now}] {message}");
        }

        /// <summary>
        /// Logs an error.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <remarks>Prepends date and time, and 'ERROR: '.</remarks>
        public void LogError(string message)
        {
            Log($"ERROR: {message}");
        }

        /// <summary>
        /// Logs a warning.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <remarks>Prepends date and time, and 'WARNING: '.</remarks>
        public void LogWarning(string message)
        {
            Log($"WARNING: {message}");
        }

        /// <summary>
        /// Logs an info message.
        /// </summary>
        /// <param name="message">The info message to log.</param>
        /// <remarks>Prepends date and time, and 'INFO: '.</remarks>
        public void LogInfo(string message)
        {
            Log($"INFO: {message}");
        }

        /// <summary>
        /// Close the file stream.
        /// </summary>
        public void Close()
        {
            sw.Close();
        }
    }
}
