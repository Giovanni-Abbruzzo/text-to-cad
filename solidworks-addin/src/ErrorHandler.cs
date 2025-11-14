using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Centralized error handling and user-friendly error messages
    /// </summary>
    public static class ErrorHandler
    {
        /// <summary>
        /// Handle exceptions and provide user-friendly messages
        /// </summary>
        /// <param name="ex">The exception to handle</param>
        /// <param name="context">Context of where the error occurred</param>
        /// <returns>User-friendly error message</returns>
        public static string HandleException(Exception ex, string context = "")
        {
            string userMessage;
            string logMessage = $"Error in {context}: {ex.Message}";

            // Log the full exception
            Logger.Error(logMessage, ex);

            // Determine user-friendly message based on exception type
            if (ex is HttpRequestException)
            {
                userMessage = "❌ Cannot connect to backend API.\n\n" +
                             "Please ensure:\n" +
                             "• Backend server is running (uvicorn main:app --reload)\n" +
                             "• API URL is correct in settings\n" +
                             "• No firewall is blocking the connection\n\n" +
                             $"Technical details: {ex.Message}";
            }
            else if (ex is TaskCanceledException || ex is TimeoutException)
            {
                userMessage = "⏱️ Request timed out.\n\n" +
                             "The backend took too long to respond.\n" +
                             "Please check if the server is overloaded or the instruction is too complex.\n\n" +
                             $"Technical details: {ex.Message}";
            }
            else if (ex is Newtonsoft.Json.JsonException)
            {
                userMessage = "📄 Invalid response from backend.\n\n" +
                             "The server returned data in an unexpected format.\n" +
                             "This might indicate a backend error or version mismatch.\n\n" +
                             $"Technical details: {ex.Message}";
            }
            else if (ex is ArgumentException || ex is ArgumentNullException)
            {
                userMessage = "⚠️ Invalid input.\n\n" +
                             $"{ex.Message}\n\n" +
                             "Please check your instruction and try again.";
            }
            else if (ex is UnauthorizedAccessException)
            {
                userMessage = "🔒 Access denied.\n\n" +
                             "The add-in doesn't have permission to perform this operation.\n" +
                             "Try running SolidWorks as administrator.\n\n" +
                             $"Technical details: {ex.Message}";
            }
            else
            {
                // Generic error
                userMessage = $"❌ An unexpected error occurred.\n\n" +
                             $"Error type: {ex.GetType().Name}\n" +
                             $"Message: {ex.Message}\n\n" +
                             $"Please check the log file for more details.";
            }

            return userMessage;
        }

        /// <summary>
        /// Show error message box to user
        /// </summary>
        public static void ShowError(string message, string title = "Text-to-CAD Error")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Show warning message box to user
        /// </summary>
        public static void ShowWarning(string message, string title = "Text-to-CAD Warning")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Show info message box to user
        /// </summary>
        public static void ShowInfo(string message, string title = "Text-to-CAD")
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Ask user for confirmation
        /// </summary>
        public static bool Confirm(string message, string title = "Confirm Action")
        {
            DialogResult result = MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            return result == DialogResult.Yes;
        }

        /// <summary>
        /// Validate instruction input
        /// </summary>
        public static bool ValidateInstruction(string instruction, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(instruction))
            {
                errorMessage = "Instruction cannot be empty.\nPlease enter a CAD command.";
                return false;
            }

            if (instruction.Length < 3)
            {
                errorMessage = "Instruction is too short.\nPlease enter at least 3 characters.";
                return false;
            }

            if (instruction.Length > 1000)
            {
                errorMessage = "Instruction is too long.\nPlease keep it under 1000 characters.";
                return false;
            }

            return true;
        }
    }
}
