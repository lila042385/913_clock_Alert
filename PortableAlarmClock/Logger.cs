// v1.00 20260617 23:18
using System;
using System.IO;
using System.Text;

namespace PortableAlarmClock
{
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string? _logDir;

        public static void Initialize(string baseDir)
        {
            lock (_lock)
            {
                _logDir = Path.Combine(baseDir, "AlarmClockData", "logs");
            }
        }

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warn(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            var sb = new StringBuilder();
            sb.Append(message);
            if (ex != null)
            {
                sb.AppendLine();
                sb.Append("Exception: ");
                sb.Append(ex.GetType().FullName);
                sb.Append(": ");
                sb.Append(ex.Message);
                sb.AppendLine();
                sb.Append("Stack Trace: ");
                sb.Append(SanitizePath(ex.StackTrace ?? string.Empty));
            }
            WriteLog("ERROR", sb.ToString());
        }

        private static void WriteLog(string level, string message)
        {
            lock (_lock)
            {
                if (_logDir == null)
                {
                    // Fallback to local directory if not initialized
                    _logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AlarmClockData", "logs");
                }

                try
                {
                    if (!Directory.Exists(_logDir))
                    {
                        Directory.CreateDirectory(_logDir);
                    }

                    string logPath = Path.Combine(_logDir, $"app-{DateTime.Now:yyyyMMdd}.log");
                    string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {SanitizePath(message)}{Environment.NewLine}";

                    // Write with UTF-8, though content should be ASCII
                    File.AppendAllText(logPath, logLine, Encoding.UTF8);
                }
                catch
                {
                    // Fail silently to avoid crashing the app
                }
            }
        }

        private static string SanitizePath(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // Remove typical developer paths, windows usernames, etc.
            // Replace "C:\Users\<username>" with "C:\Users\<User>" to protect privacy.
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile))
            {
                input = input.Replace(userProfile, "%USERPROFILE%");
            }

            // Remove specific git/workspace directories if present in exception callstack
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                input = input.Replace(baseDir, ".\\");
            }

            return input;
        }
    }
}
// v1.00 20260617 23:18
