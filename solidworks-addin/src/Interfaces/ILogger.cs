using System;

namespace TextToCad.SolidWorksAddin.Interfaces
{
    /// <summary>
    /// Lightweight logging interface for add-in operations.
    /// Provides basic logging methods without external dependencies.
    /// </summary>
    /// <remarks>
    /// This interface allows for flexible logging implementations:
    /// - Can forward to UI controls (e.g., Task Pane log)
    /// - Can write to debug output or files
    /// - Can be mocked for unit testing
    /// </remarks>
    public interface ILogger
    {
        /// <summary>
        /// Log an informational message.
        /// Used for normal operation flow and status updates.
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <example>
        /// logger.Info("Sketch created successfully");
        /// </example>
        void Info(string message);

        /// <summary>
        /// Log a warning message.
        /// Used for non-critical issues that don't prevent operation but should be noted.
        /// </summary>
        /// <param name="message">The warning message to log</param>
        /// <example>
        /// logger.Warn("Face selection returned null, using default plane");
        /// </example>
        void Warn(string message);

        /// <summary>
        /// Log an error message.
        /// Used for failures and exceptions that prevent normal operation.
        /// </summary>
        /// <param name="message">The error message to log</param>
        /// <example>
        /// logger.Error("Failed to create extrude feature: " + ex.Message);
        /// </example>
        void Error(string message);
    }
}
