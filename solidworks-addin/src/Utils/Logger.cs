using System;
using System.Diagnostics;
using TextToCad.SolidWorksAddin.Interfaces;

namespace TextToCad.SolidWorksAddin.Utils
{
    /// <summary>
    /// Simple thread-safe logger implementation.
    /// Forwards messages to a custom sink or falls back to Debug output.
    /// </summary>
    /// <remarks>
    /// This is a lightweight logger suitable for add-in utilities.
    /// For file-based logging, see the existing Logger class in the root namespace.
    /// This implementation is designed for real-time feedback to UI controls.
    /// </remarks>
    public class Logger : ILogger
    {
        private readonly Action<string> _sink;
        private readonly object _lock = new object();

        /// <summary>
        /// Create a logger with an optional message sink.
        /// </summary>
        /// <param name="sink">
        /// Optional callback to receive log messages (e.g., append to TextBox).
        /// If null, messages are written to Debug output.
        /// </param>
        /// <example>
        /// // Forward to Task Pane log:
        /// var logger = new Logger(msg => txtLog.AppendText(msg + "\r\n"));
        /// 
        /// // Use Debug output:
        /// var logger = new Logger();
        /// </example>
        public Logger(Action<string> sink = null)
        {
            _sink = sink;
        }

        /// <summary>
        /// Log an informational message with [INFO] prefix.
        /// </summary>
        /// <param name="message">The message to log</param>
        public void Info(string message)
        {
            Log("[INFO]", message);
        }

        /// <summary>
        /// Log a warning message with [WARN] prefix.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public void Warn(string message)
        {
            Log("[WARN]", message);
        }

        /// <summary>
        /// Log an error message with [ERROR] prefix.
        /// </summary>
        /// <param name="message">The error message to log</param>
        public void Error(string message)
        {
            Log("[ERROR]", message);
        }

        /// <summary>
        /// Internal thread-safe logging method.
        /// </summary>
        /// <param name="level">Log level prefix</param>
        /// <param name="message">The message content</param>
        private void Log(string level, string message)
        {
            // Format with timestamp
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] {level} {message}";

            // Thread-safe append
            lock (_lock)
            {
                if (_sink != null)
                {
                    try
                    {
                        _sink(formattedMessage);
                    }
                    catch (Exception ex)
                    {
                        // Fallback to System.Diagnostics.Debug if sink fails
                        System.Diagnostics.Debug.WriteLine($"Logger sink failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine(formattedMessage);
                    }
                }
                else
                {
                    // Default to System.Diagnostics.Debug output
                    System.Diagnostics.Debug.WriteLine(formattedMessage);
                }
            }
        }

        /// <summary>
        /// Create a null logger that discards all messages.
        /// Useful for optional logger parameters.
        /// </summary>
        /// <returns>A logger instance that does nothing</returns>
        public static ILogger Null()
        {
            return new Logger(_ => { /* discard */ });
        }

        /// <summary>
        /// Create a logger that writes to Debug output.
        /// </summary>
        /// <returns>A logger instance using Debug.WriteLine</returns>
        public static ILogger Debug()
        {
            return new Logger();
        }
    }
}
