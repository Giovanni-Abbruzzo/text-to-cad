using System;
using System.Drawing;
using System.Windows.Forms;
using TextToCad.SolidWorksAddin.Models;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Main UI control for the Task Pane.
    /// Provides interface for entering instructions and viewing results.
    /// </summary>
    public partial class TaskPaneControl : UserControl
    {
        #region Private Fields

        private bool isProcessing = false;

        #endregion

        #region Constructor

        public TaskPaneControl()
        {
            InitializeComponent();
            InitializeUI();
            Logger.Info("TaskPaneControl initialized");
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// Additional UI initialization beyond the designer
        /// </summary>
        private void InitializeUI()
        {
            // Set initial API URL from config
            txtApiBase.Text = ApiClient.GetBaseUrl();

            // Set placeholder text styling
            txtInstruction.ForeColor = SystemColors.GrayText;
            txtInstruction.Text = "Enter CAD instruction here... (e.g., 'create 4 holes in a circular pattern')";

            // Update connection status
            UpdateConnectionStatus();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle Preview button click - calls /dry_run endpoint
        /// </summary>
        private async void btnPreview_Click(object sender, EventArgs e)
        {
            // Validate instruction
            if (!ErrorHandler.ValidateInstruction(txtInstruction.Text, out string errorMessage))
            {
                AppendLog("❌ Validation Error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("⏳ Already processing a request...", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog("🔍 Previewing instruction...", Color.Blue);
                Logger.Info($"Preview requested: '{txtInstruction.Text}'");

                // Create request
                var request = new InstructionRequest(txtInstruction.Text, chkUseAI.Checked);

                // Call API
                var response = await ApiClient.DryRunAsync(request);

                // Display results
                DisplayResponse(response, isPreview: true);

                AppendLog("✓ Preview complete", Color.Green);
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Preview");
                AppendLog("❌ Preview Failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Execute button click - calls /process_instruction endpoint
        /// </summary>
        private async void btnExecute_Click(object sender, EventArgs e)
        {
            // Validate instruction
            if (!ErrorHandler.ValidateInstruction(txtInstruction.Text, out string errorMessage))
            {
                AppendLog("❌ Validation Error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("⏳ Already processing a request...", Color.Orange);
                return;
            }

            // Confirm execution
            if (!ErrorHandler.Confirm(
                $"Execute this instruction?\n\n\"{txtInstruction.Text}\"\n\n" +
                "This will save the command to the database.",
                "Confirm Execution"))
            {
                AppendLog("❌ Execution cancelled by user", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog("⚙️ Executing instruction...", Color.Blue);
                Logger.Info($"Execute requested: '{txtInstruction.Text}'");

                // Create request
                var request = new InstructionRequest(txtInstruction.Text, chkUseAI.Checked);

                // Call API
                var response = await ApiClient.ProcessInstructionAsync(request);

                // Display results
                DisplayResponse(response, isPreview: false);

                AppendLog("✓ Execution complete (saved to database)", Color.Green);
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Execute");
                AppendLog("❌ Execution Failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Clear Log button click
        /// </summary>
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            txtLog.Clear();
            txtPlan.Clear();
            lblStatus.Text = "Ready";
            lblStatus.ForeColor = SystemColors.ControlText;
            Logger.Debug("Log cleared");
        }

        /// <summary>
        /// Handle API URL change
        /// </summary>
        private void btnUpdateUrl_Click(object sender, EventArgs e)
        {
            try
            {
                string newUrl = txtApiBase.Text.Trim();
                ApiClient.SetBaseUrl(newUrl);
                AppendLog($"✓ API URL updated to: {newUrl}", Color.Green);
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Invalid URL: {ex.Message}", Color.Red);
            }
        }

        /// <summary>
        /// Handle Test Connection button click
        /// </summary>
        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            try
            {
                SetUIEnabled(false);
                AppendLog("🔌 Testing connection...", Color.Blue);

                bool connected = await ApiClient.TestConnectionAsync();

                if (connected)
                {
                    AppendLog("✓ Connection successful!", Color.Green);
                    UpdateConnectionStatus(true);
                }
                else
                {
                    AppendLog("❌ Connection failed - server not responding", Color.Red);
                    UpdateConnectionStatus(false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Connection error: {ex.Message}", Color.Red);
                UpdateConnectionStatus(false);
            }
            finally
            {
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Open Logs button click
        /// </summary>
        private void btnOpenLogs_Click(object sender, EventArgs e)
        {
            Logger.OpenLogDirectory();
        }

        /// <summary>
        /// Handle instruction textbox focus - clear placeholder
        /// </summary>
        private void txtInstruction_Enter(object sender, EventArgs e)
        {
            if (txtInstruction.ForeColor == SystemColors.GrayText)
            {
                txtInstruction.Text = "";
                txtInstruction.ForeColor = SystemColors.WindowText;
            }
        }

        /// <summary>
        /// Handle instruction textbox blur - restore placeholder if empty
        /// </summary>
        private void txtInstruction_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtInstruction.Text))
            {
                txtInstruction.ForeColor = SystemColors.GrayText;
                txtInstruction.Text = "Enter CAD instruction here... (e.g., 'create 4 holes in a circular pattern')";
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Display API response in the UI
        /// </summary>
        private void DisplayResponse(InstructionResponse response, bool isPreview)
        {
            string mode = isPreview ? "PREVIEW" : "EXECUTE";
            string source = response.IsAIParsed ? "AI" : "Rule-based";

            AppendLog($"\n═══════════════════════════════════", Color.DarkBlue);
            AppendLog($"  {mode} RESULTS", Color.DarkBlue);
            AppendLog($"═══════════════════════════════════", Color.DarkBlue);
            AppendLog($"Source: {source} parsing", Color.DarkGray);
            AppendLog($"Schema Version: {response.SchemaVersion}", Color.DarkGray);
            AppendLog("");

            // Display plan
            AppendLog("📋 PLAN:", Color.DarkBlue);
            txtPlan.Clear();
            if (response.Plan != null && response.Plan.Count > 0)
            {
                foreach (var step in response.Plan)
                {
                    AppendLog($"  • {step}", Color.Black);
                    txtPlan.AppendText($"• {step}\r\n");
                }
            }
            else
            {
                AppendLog("  (No plan available)", Color.Gray);
            }

            AppendLog("");

            // Display parsed parameters
            if (response.ParsedParameters != null)
            {
                AppendLog("🔧 PARSED PARAMETERS:", Color.DarkBlue);
                AppendLog($"  Action: {response.ParsedParameters.GetActionDescription()}", Color.Black);
                AppendLog($"  {response.ParsedParameters.GetParametersSummary()}", Color.Black);
            }

            AppendLog($"═══════════════════════════════════\n", Color.DarkBlue);

            // Update status
            lblStatus.Text = $"{mode} Complete - {source}";
            lblStatus.ForeColor = Color.Green;
        }

        /// <summary>
        /// Append text to log with color
        /// </summary>
        private void AppendLog(string text, Color color)
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(text + "\r\n");
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// Enable/disable UI controls during processing
        /// </summary>
        private void SetUIEnabled(bool enabled)
        {
            btnPreview.Enabled = enabled;
            btnExecute.Enabled = enabled;
            btnUpdateUrl.Enabled = enabled;
            btnTestConnection.Enabled = enabled;
            txtInstruction.Enabled = enabled;
            txtApiBase.Enabled = enabled;
            chkUseAI.Enabled = enabled;

            if (!enabled)
            {
                lblStatus.Text = "Processing...";
                lblStatus.ForeColor = Color.Orange;
            }
        }

        /// <summary>
        /// Update connection status indicator
        /// </summary>
        private async void UpdateConnectionStatus(bool? forceStatus = null)
        {
            bool connected;
            
            if (forceStatus.HasValue)
            {
                connected = forceStatus.Value;
            }
            else
            {
                try
                {
                    connected = await ApiClient.TestConnectionAsync();
                }
                catch
                {
                    connected = false;
                }
            }

            if (connected)
            {
                lblConnectionStatus.Text = "● Connected";
                lblConnectionStatus.ForeColor = Color.Green;
            }
            else
            {
                lblConnectionStatus.Text = "● Disconnected";
                lblConnectionStatus.ForeColor = Color.Red;
            }
        }

        #endregion
    }
}
