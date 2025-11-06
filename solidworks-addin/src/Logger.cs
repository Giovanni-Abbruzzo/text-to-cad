using System;
using System.Configuration;
using System.IO;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Centralized logging system for the add-in.
    /// Logs to both file and provides in-memory log for UI display.
    /// </summary>
    public static class Logger
    {
        private static readonly string LogDirectory;
        private static readonly string LogFilePath;
        private static readonly bool FileLoggingEnabled;
        private static readonly object LockObject = new object();

        /// <summary>
        /// Log levels for filtering
        /// </summary>
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3
        }

        private static LogLevel CurrentLogLevel = LogLevel.Info;

        static Logger()
        {
            try
            {
                // Read configuration
                FileLoggingEnabled = bool.Parse(ConfigurationManager.AppSettings["EnableFileLogging"] ?? "true");
                string logLevelStr = ConfigurationManager.AppSettings["LogLevel"] ?? "Info";
                Enum.TryParse(logLevelStr, out CurrentLogLevel);

                // Set up log directory in AppData
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                LogDirectory = Path.Combine(appDataPath, "TextToCad", "logs");
                
                // Create directory if it doesn't exist
                if (FileLoggingEnabled && !Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // Create log file with timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd");
                LogFilePath = Path.Combine(LogDirectory, $"TextToCad_{timestamp}.log");
            }
            catch (Exception ex)
            {
                // If logging setup fails, disable file logging
                FileLoggingEnabled = false;
                System.Diagnostics.Debug.WriteLine($"Logger initialization failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Log a debug message
        /// </summary>
        public static void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        /// <summary>
        /// Log an info message
        /// </summary>
        public static void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message, Exception ex = null)
        {
            string fullMessage = message;
            if (ex != null)
            {
                fullMessage += $"\nException: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}";
            }
            Log(LogLevel.Error, fullMessage);
        }

        /// <summary>
        /// Core logging method
        /// </summary>
        private static void Log(LogLevel level, string message)
        {
            // Check if we should log this level
            if (level < CurrentLogLevel)
                return;

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] [{level}] {message}";

            // Always write to debug output
            System.Diagnostics.Debug.WriteLine(logEntry);

            // Write to file if enabled
            if (FileLoggingEnabled)
            {
                try
                {
                    lock (LockObject)
                    {
                        File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Get the current log file path
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// Open the log directory in Windows Explorer
        /// </summary>
        public static void OpenLogDirectory()
        {
            try
            {
                if (Directory.Exists(LogDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", LogDirectory);
                }
            }
            catch (Exception ex)
            {
                Error("Failed to open log directory", ex);
            }
        }
    }
}
