using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TextToCad.SolidWorksAddin.Models;
using TextToCad.SolidWorksAddin.Utils;

namespace TextToCad.SolidWorksAddin
{
    /// <summary>
    /// Main UI control for the Task Pane.
    /// Provides interface for entering instructions and viewing results.
    /// </summary>
    [ComVisible(true)]
    public partial class TaskPaneControl : UserControl
    {
        #region Private Fields

        private bool isProcessing = false;
        private Addin _addin;
        private InstructionResponse _lastResponse;
        private bool _lastUseAI;
        private readonly List<ParsedParameters> _stepOperations = new List<ParsedParameters>();
        private readonly HashSet<int> _executedSteps = new HashSet<int>();
        private readonly Stack<int> _undoStack = new Stack<int>();
        private readonly Dictionary<int, string> _stepFeatureNames = new Dictionary<int, string>();
        private string _plannerStateId;
        private PlannerResponse _plannerResponse;
        private SpeechRecognitionEngine _speechRecognizer;
        private bool _isVoiceRecording;
        private string _lastVoiceTranscript;
        private bool _isInitializingVoice;
        private string _whisperExePath;
        private string _whisperModelPath;
        private string _whisperLanguage;
        private string _voiceRecordingPath;
        private const string VoiceRecordingAlias = "TextToCadVoice";

        #endregion

        #region Constructor

        public TaskPaneControl()
        {
            InitializeComponent();
            InitializeUI();
            Logger.Info("TaskPaneControl initialized");
        }

        /// <summary>
        /// Set the add-in reference for accessing SolidWorks API
        /// </summary>
        public void SetAddin(Addin addin)
        {
            _addin = addin;
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

            // Update replay status label
            UpdateReplayStatusLabel();

            if (txtPlannerQuestions != null)
            {
                txtPlannerQuestions.Text = "Planner idle. Click Plan to start.";
            }

            if (lblPlannerState != null)
            {
                lblPlannerState.Text = "Planner: idle";
            }

            InitializeVoice();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handle Plan button click - calls /plan endpoint
        /// </summary>
        private async void btnPlan_Click(object sender, EventArgs e)
        {
            // Validate instruction
            if (!ErrorHandler.ValidateInstruction(txtInstruction.Text, out string errorMessage))
            {
                AppendLog("Validation error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                string instructionText = txtInstruction.Text.Trim();
                AppendLog("Planning instruction...", Color.Blue);
                Logger.Info($"Plan requested: '{instructionText}'");

                var request = new PlannerRequest
                {
                    Instruction = instructionText,
                    UseAI = chkUseAI.Checked
                };

                var response = await ApiClient.PlanAsync(request);
                DisplayPlannerResponse(response);

                AppendLog("Planner response received", Color.Green);
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Planner");
                AppendLog("Planner request failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Submit Answers button click - calls /plan with state_id
        /// </summary>
        private async void btnPlannerSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_plannerStateId))
            {
                AppendLog("Planner is idle. Click Plan to start.", Color.Orange);
                return;
            }

            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            var answers = ParsePlannerAnswers(txtPlannerAnswers.Text);
            if (answers.Count == 0)
            {
                AppendLog("No planner answers provided. Use key=value per line.", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog($"Submitting planner answers (state {_plannerStateId})...", Color.Blue);

                var request = new PlannerRequest
                {
                    StateId = _plannerStateId,
                    Answers = answers,
                    UseAI = chkUseAI.Checked
                };

                var response = await ApiClient.PlanAsync(request);
                DisplayPlannerResponse(response);

                AppendLog("Planner updated", Color.Green);
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Planner");
                AppendLog("Planner update failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Preview button click - calls /dry_run endpoint
        /// </summary>
        private async void btnPreview_Click(object sender, EventArgs e)
        {
            // Validate instruction
            if (!ErrorHandler.ValidateInstruction(txtInstruction.Text, out string errorMessage))
            {
                AppendLog("Validation error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog("Previewing instruction...", Color.Blue);
                Logger.Info($"Preview requested: '{txtInstruction.Text}'");

                // Create request
                var request = new InstructionRequest(txtInstruction.Text, chkUseAI.Checked);

                // Call API
                var response = await ApiClient.DryRunAsync(request);

                // Display results
                DisplayResponse(response, isPreview: true);

                AppendLog("Preview complete", Color.Green);
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Preview");
                AppendLog("Preview failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Execute button click - uses /process_instruction and then executes CAD
        /// </summary>
        private async void btnExecute_Click(object sender, EventArgs e)
        {
            await ExecuteInstructionAsync(txtInstruction.Text.Trim(), requireConfirm: true, sourceLabel: "Execute");
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
                AppendLog($"API URL updated to: {newUrl}", Color.Green);
                UpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                AppendLog($"Invalid URL: {ex.Message}", Color.Red);
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
                AppendLog("Testing connection...", Color.Blue);

                bool connected = await ApiClient.TestConnectionAsync();

                if (connected)
                {
                    AppendLog("Connection successful", Color.Green);
                    UpdateConnectionStatus(true);
                }
                else
                {
                    AppendLog("Connection failed - server not responding", Color.Red);
                    UpdateConnectionStatus(false);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Connection error: {ex.Message}", Color.Red);
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
        /// Handle Replay Last Session button click
        /// </summary>
        private void btnReplayLast_Click(object sender, EventArgs e)
        {
            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog("Replaying last session...", Color.Blue);
                Logger.Info("Replay last session requested");

                TryExecuteReplayCommand("replay last");
            }
            catch (Exception ex)
            {
                AppendLog($"Replay failed: {ex.Message}", Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Open Replay Folder button click
        /// </summary>
        private void btnOpenReplay_Click(object sender, EventArgs e)
        {
            ReplayLogger.OpenReplayDirectory();
        }

        /// <summary>
        /// Handle Start Replay Session button click
        /// </summary>
        private void btnReplayStart_Click(object sender, EventArgs e)
        {
            if (ReplayLogger.BeginSession(out string sessionId, out string replayPath))
            {
                AppendLog($"Replay session started: {sessionId}", Color.Green);
                AppendLog($"Replay log: {replayPath}", Color.DarkGray);
                UpdateReplayStatusLabel();
            }
            else
            {
                AppendLog("Replay logging is disabled.", Color.Orange);
            }
        }

        /// <summary>
        /// Handle Pause Replay Session button click
        /// </summary>
        private void btnReplayPause_Click(object sender, EventArgs e)
        {
            if (!ReplayLogger.IsSessionActive())
            {
                AppendLog("No active replay session to pause.", Color.Orange);
                return;
            }

            ReplayLogger.PauseSession();
            AppendLog("Replay session paused.", Color.Orange);
            UpdateReplayStatusLabel();
        }

        /// <summary>
        /// Handle Resume Replay Session button click
        /// </summary>
        private void btnReplayResume_Click(object sender, EventArgs e)
        {
            if (!ReplayLogger.IsSessionActive())
            {
                AppendLog("No active replay session to resume.", Color.Orange);
                return;
            }

            ReplayLogger.ResumeSession();
            AppendLog("Replay session resumed.", Color.Green);
            UpdateReplayStatusLabel();
        }

        /// <summary>
        /// Handle End Replay Session button click
        /// </summary>
        private void btnReplayEnd_Click(object sender, EventArgs e)
        {
            if (!ReplayLogger.IsSessionActive())
            {
                AppendLog("No active replay session to end.", Color.Orange);
                return;
            }

            ReplayLogger.EndSession();
            AppendLog("Replay session ended.", Color.Orange);
            UpdateReplayStatusLabel();
        }

        /// <summary>
        /// Handle Run Selected Step button click
        /// </summary>
        private void btnRunSelectedStep_Click(object sender, EventArgs e)
        {
            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            if (lstSteps == null || lstSteps.SelectedIndex < 0)
            {
                AppendLog("Select a step to run.", Color.Orange);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);
                ExecuteStepIndices(new List<int> { lstSteps.SelectedIndex });
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Run Checked Steps button click
        /// </summary>
        private void btnRunCheckedSteps_Click(object sender, EventArgs e)
        {
            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            if (lstSteps == null || lstSteps.CheckedIndices.Count == 0)
            {
                AppendLog("No checked steps to run.", Color.Orange);
                return;
            }

            var indices = lstSteps.CheckedIndices.Cast<int>().OrderBy(i => i).ToList();
            try
            {
                isProcessing = true;
                SetUIEnabled(false);
                ExecuteStepIndices(indices);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Run Next Step button click
        /// </summary>
        private void btnRunNextStep_Click(object sender, EventArgs e)
        {
            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            if (_stepOperations.Count == 0)
            {
                AppendLog("No steps loaded. Preview or Execute an instruction first.", Color.Orange);
                return;
            }

            int nextIndex = -1;
            for (int i = 0; i < _stepOperations.Count; i++)
            {
                if (_executedSteps.Contains(i))
                {
                    continue;
                }

                if (_stepOperations[i] == null)
                {
                    continue;
                }

                nextIndex = i;
                break;
            }

            if (nextIndex < 0)
            {
                AppendLog("All steps have already been executed.", Color.DarkGray);
                return;
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);
                ExecuteStepIndices(new List<int> { nextIndex });
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Handle Undo Last Step button click
        /// </summary>
        private void btnUndoLastStep_Click(object sender, EventArgs e)
        {
            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            if (_undoStack.Count == 0)
            {
                AppendLog("No executed steps to undo.", Color.Orange);
                return;
            }

            if (!TryGetActiveModel(out var swApp, out var model, out var logger))
            {
                return;
            }

            int lastIndex = _undoStack.Peek();
            bool undone = TryUndoLastOperation(model, logger, lastIndex);
            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                if (undone)
                {
                    _undoStack.Pop();
                    _executedSteps.Remove(lastIndex);
                    _stepFeatureNames.Remove(lastIndex);
                    if (lastIndex >= 0 && lastIndex < _stepOperations.Count)
                    {
                        lstSteps.Items[lastIndex] = BuildStepLabel(lastIndex, _stepOperations[lastIndex], false);
                    }
                    AppendLog($"Undo successful for step {lastIndex + 1}.", Color.Green);
                }
                else
                {
                    AppendLog("Undo failed - see log for details.", Color.Red);
                }
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        private void UpdateReplayStatusLabel()
        {
            if (lblReplayStatus == null)
            {
                return;
            }

            if (ReplayLogger.IsSessionActive())
            {
                int sessionIndex = ReplayLogger.GetSessionIndex();
                if (ReplayLogger.IsPaused())
                {
                    lblReplayStatus.Text = $"Replay paused: session {sessionIndex} (resume to continue logging)";
                }
                else
                {
                    lblReplayStatus.Text = $"Tracking commands for session {sessionIndex}.";
                }
            }
            else
            {
                int lastSession = ReplayLogger.GetLastSessionIndex();
                if (lastSession > 0)
                {
                    lblReplayStatus.Text = $"Replay idle. Click 'Replay Last Session' to recreate session {lastSession}.";
                }
                else
                {
                    lblReplayStatus.Text = "Replay idle. Click 'Replay Last Session' to recreate the last session.";
                }
            }
        }

        /// <summary>
        /// Initialize voice UI defaults.
        /// </summary>
        private void InitializeVoice()
        {
            _isInitializingVoice = true;
            _lastVoiceTranscript = string.Empty;
            if (lblVoiceStatus != null)
            {
                lblVoiceStatus.Text = "Voice: idle";
            }

            if (btnVoiceConfirm != null)
            {
                btnVoiceConfirm.Enabled = false;
            }

            if (btnVoiceCancel != null)
            {
                btnVoiceCancel.Enabled = false;
            }

            _whisperExePath = AddinConfig.Get("WhisperCliPath", string.Empty);
            _whisperModelPath = AddinConfig.Get("WhisperModelPath", string.Empty);
            _whisperLanguage = AddinConfig.Get("WhisperLanguage", "en");
            if (chkUseWhisper != null)
            {
                string useWhisperSetting = AddinConfig.Get("UseWhisper", string.Empty);
                chkUseWhisper.Checked = string.Equals(useWhisperSetting, "true", StringComparison.OrdinalIgnoreCase);
            }

            _isInitializingVoice = false;
            UpdateWhisperAvailability();
        }

        private bool IsWhisperEnabled()
        {
            return chkUseWhisper != null && chkUseWhisper.Checked;
        }

        private bool ValidateWhisperConfig(out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(_whisperExePath))
            {
                message = "Whisper is enabled but WhisperCliPath is not set in app.config.";
                return false;
            }

            if (!File.Exists(_whisperExePath))
            {
                message = $"Whisper CLI not found at: {_whisperExePath}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_whisperModelPath))
            {
                message = "Whisper is enabled but WhisperModelPath is not set in app.config.";
                return false;
            }

            if (!File.Exists(_whisperModelPath))
            {
                message = $"Whisper model not found at: {_whisperModelPath}";
                return false;
            }

            return true;
        }

        private void UpdateWhisperAvailability()
        {
            if (chkUseWhisper == null || !chkUseWhisper.Checked)
            {
                return;
            }

            if (!ValidateWhisperConfig(out string message))
            {
                if (!_isInitializingVoice)
                {
                    AppendLog(message, Color.Orange);
                }
                chkUseWhisper.Checked = false;
                UpdateVoiceStatus("Voice: idle");
                return;
            }

            UpdateVoiceStatus("Voice: whisper ready");
        }

        private bool EnsureSpeechRecognizer()
        {
            if (_speechRecognizer != null)
            {
                return true;
            }

            try
            {
                try
                {
                    _speechRecognizer = new SpeechRecognitionEngine(CultureInfo.CurrentCulture);
                }
                catch (Exception)
                {
                    _speechRecognizer = new SpeechRecognitionEngine(new CultureInfo("en-US"));
                }

                _speechRecognizer.SetInputToDefaultAudioDevice();
                _speechRecognizer.LoadGrammar(new DictationGrammar());
                _speechRecognizer.SpeechRecognized += OnSpeechRecognized;
                _speechRecognizer.SpeechRecognitionRejected += OnSpeechRejected;
                _speechRecognizer.RecognizeCompleted += OnSpeechCompleted;

                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"Voice init failed: {ex.Message}", Color.Red);
                UpdateVoiceStatus("Voice: unavailable");
                if (btnVoiceRecord != null)
                {
                    btnVoiceRecord.Enabled = false;
                }
                return false;
            }
        }

        private void StartVoiceCapture()
        {
            if (_isVoiceRecording)
            {
                return;
            }

            if (IsWhisperEnabled())
            {
                StartWhisperCapture();
                return;
            }

            StartSpeechCapture();
        }

        private void StartSpeechCapture()
        {
            if (!EnsureSpeechRecognizer())
            {
                return;
            }

            _lastVoiceTranscript = string.Empty;
            txtVoiceTranscript.Text = string.Empty;
            btnVoiceRecord.Text = "Stop";
            btnVoiceConfirm.Enabled = false;
            btnVoiceCancel.Enabled = true;
            UpdateVoiceStatus("Voice: listening...");

            try
            {
                _speechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                _isVoiceRecording = true;
            }
            catch (Exception ex)
            {
                AppendLog($"Voice start failed: {ex.Message}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                btnVoiceRecord.Text = "Record";
            }
        }

        private void StopVoiceCapture(bool transcribe = true)
        {
            if (IsWhisperEnabled())
            {
                StopWhisperCapture(transcribe);
                return;
            }

            StopSpeechCapture();
        }

        private void StopSpeechCapture()
        {
            if (!_isVoiceRecording || _speechRecognizer == null)
            {
                return;
            }

            try
            {
                _speechRecognizer.RecognizeAsyncStop();
            }
            catch (Exception)
            {
                _speechRecognizer.RecognizeAsyncCancel();
            }

            _isVoiceRecording = false;
            btnVoiceRecord.Text = "Record";
            UpdateVoiceStatus("Voice: stopped");
        }

        [DllImport("winmm.dll")]
        private static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr hwndCallback);

        [DllImport("winmm.dll")]
        private static extern bool mciGetErrorString(int errorCode, StringBuilder errorText, int errorTextSize);

        private bool TryMciCommand(string command, out string error)
        {
            error = string.Empty;
            int result = mciSendString(command, null, 0, IntPtr.Zero);
            if (result == 0)
            {
                return true;
            }

            var errorText = new StringBuilder(256);
            if (mciGetErrorString(result, errorText, errorText.Capacity))
            {
                error = errorText.ToString();
            }
            else
            {
                error = $"MCI error {result} for command: {command}";
            }

            return false;
        }

        private void StartWhisperCapture()
        {
            if (!ValidateWhisperConfig(out string message))
            {
                AppendLog(message, Color.Orange);
                UpdateVoiceStatus("Voice: whisper not configured");
                return;
            }

            _lastVoiceTranscript = string.Empty;
            txtVoiceTranscript.Text = string.Empty;
            _voiceRecordingPath = Path.Combine(Path.GetTempPath(), $"texttocad_voice_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            if (!TryMciCommand($"open new Type waveaudio Alias {VoiceRecordingAlias}", out string error))
            {
                AppendLog($"Voice record start failed: {error}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                return;
            }

            if (!TryMciCommand($"record {VoiceRecordingAlias}", out error))
            {
                AppendLog($"Voice record start failed: {error}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                TryMciCommand($"close {VoiceRecordingAlias}", out _);
                return;
            }

            _isVoiceRecording = true;
            btnVoiceRecord.Text = "Stop";
            btnVoiceConfirm.Enabled = false;
            btnVoiceCancel.Enabled = true;
            UpdateVoiceStatus("Voice: recording (whisper)...");
        }

        private void StopWhisperCapture(bool transcribe)
        {
            if (!_isVoiceRecording)
            {
                return;
            }

            if (!TryMciCommand($"save {VoiceRecordingAlias} \"{_voiceRecordingPath}\"", out string error))
            {
                AppendLog($"Voice record save failed: {error}", Color.Red);
            }

            TryMciCommand($"close {VoiceRecordingAlias}", out _);

            _isVoiceRecording = false;
            btnVoiceRecord.Text = "Record";

            if (!transcribe)
            {
                UpdateVoiceStatus("Voice: stopped");
                return;
            }

            if (string.IsNullOrWhiteSpace(_voiceRecordingPath) || !File.Exists(_voiceRecordingPath))
            {
                AppendLog("Voice recording not found for transcription.", Color.Red);
                UpdateVoiceStatus("Voice: error");
                return;
            }

            btnVoiceRecord.Enabled = false;
            UpdateVoiceStatus("Voice: transcribing...");
            _ = TranscribeWhisperAsync(_voiceRecordingPath);
        }

        private async Task TranscribeWhisperAsync(string wavPath)
        {
            string transcript = string.Empty;
            string errorMessage = string.Empty;

            try
            {
                transcript = await Task.Run(() => RunWhisperCli(wavPath, out errorMessage));
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            if (string.IsNullOrWhiteSpace(transcript))
            {
                AppendLog($"Whisper transcription failed: {errorMessage}", Color.Red);
                UpdateVoiceStatus("Voice: error");
                btnVoiceRecord.Enabled = true;
                return;
            }

            _lastVoiceTranscript = transcript.Trim();
            SetVoiceTranscript(_lastVoiceTranscript);
            UpdateVoiceStatus("Voice: captured (whisper)");

            if (btnVoiceConfirm.InvokeRequired)
            {
                btnVoiceConfirm.BeginInvoke(new Action(() => btnVoiceConfirm.Enabled = true));
            }
            else
            {
                btnVoiceConfirm.Enabled = true;
            }

            btnVoiceRecord.Enabled = true;
        }

        private string RunWhisperCli(string wavPath, out string error)
        {
            error = string.Empty;

            if (!ValidateWhisperConfig(out error))
            {
                return string.Empty;
            }

            string outputBase = Path.Combine(Path.GetTempPath(), $"texttocad_whisper_{Guid.NewGuid()}");
            string outputTxt = outputBase + ".txt";
            string args = $"-m \"{_whisperModelPath}\" -f \"{wavPath}\" -l {_whisperLanguage} -otxt -of \"{outputBase}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = _whisperExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    error = "Failed to start Whisper CLI process.";
                    return string.Empty;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(error))
                {
                    error = string.IsNullOrWhiteSpace(stderr) ? $"Whisper exited with code {process.ExitCode}." : stderr.Trim();
                }

                if (File.Exists(outputTxt))
                {
                    return File.ReadAllText(outputTxt).Trim();
                }

                if (!string.IsNullOrWhiteSpace(stdout))
                {
                    return stdout.Trim();
                }
            }

            if (string.IsNullOrWhiteSpace(error))
            {
                error = "Whisper did not return any transcript.";
            }

            return string.Empty;
        }

        private void UpdateVoiceStatus(string status)
        {
            if (lblVoiceStatus == null)
            {
                return;
            }

            if (lblVoiceStatus.InvokeRequired)
            {
                lblVoiceStatus.BeginInvoke(new Action(() => lblVoiceStatus.Text = status));
                return;
            }

            lblVoiceStatus.Text = status;
        }

        private void SetVoiceTranscript(string transcript)
        {
            if (txtVoiceTranscript == null)
            {
                return;
            }

            if (txtVoiceTranscript.InvokeRequired)
            {
                txtVoiceTranscript.BeginInvoke(new Action(() => txtVoiceTranscript.Text = transcript));
                return;
            }

            txtVoiceTranscript.Text = transcript;
        }

        private void OnSpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result == null || string.IsNullOrWhiteSpace(e.Result.Text))
            {
                return;
            }

            _lastVoiceTranscript = e.Result.Text.Trim();
            SetVoiceTranscript(_lastVoiceTranscript);
            UpdateVoiceStatus($"Voice: captured ({Math.Round(e.Result.Confidence * 100)}% confidence)");

            if (btnVoiceConfirm.InvokeRequired)
            {
                btnVoiceConfirm.BeginInvoke(new Action(() => btnVoiceConfirm.Enabled = true));
            }
            else
            {
            btnVoiceConfirm.Enabled = true;
            }
        }

        private void chkUseWhisper_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInitializingVoice)
            {
                return;
            }

            UpdateWhisperAvailability();
        }

        private void OnSpeechRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            UpdateVoiceStatus("Voice: no match");
        }

        private void OnSpeechCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (_isVoiceRecording)
            {
                return;
            }

            UpdateVoiceStatus("Voice: idle");
        }

        private void btnVoiceRecord_Click(object sender, EventArgs e)
        {
            if (_isVoiceRecording)
            {
                StopVoiceCapture();
            }
            else
            {
                StartVoiceCapture();
            }
        }

        private async void btnVoiceConfirm_Click(object sender, EventArgs e)
        {
            string transcript = txtVoiceTranscript?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(transcript))
            {
                AppendLog("Voice transcript is empty.", Color.Orange);
                return;
            }

            StopVoiceCapture(transcribe: false);
            _lastVoiceTranscript = transcript;
            txtInstruction.ForeColor = SystemColors.ControlText;
            txtInstruction.Text = transcript;

            await ExecuteInstructionAsync(transcript, requireConfirm: false, sourceLabel: "Voice");
        }

        private void btnVoiceCancel_Click(object sender, EventArgs e)
        {
            StopVoiceCapture(transcribe: false);
            _lastVoiceTranscript = string.Empty;
            SetVoiceTranscript(string.Empty);
            btnVoiceConfirm.Enabled = false;
            btnVoiceCancel.Enabled = false;
            UpdateVoiceStatus("Voice: idle");
        }

        private void txtVoiceTranscript_TextChanged(object sender, EventArgs e)
        {
            if (txtVoiceTranscript == null)
            {
                return;
            }

            _lastVoiceTranscript = txtVoiceTranscript.Text.Trim();
            if (btnVoiceConfirm != null)
            {
                btnVoiceConfirm.Enabled = !string.IsNullOrWhiteSpace(_lastVoiceTranscript);
            }
        }

        private async System.Threading.Tasks.Task ExecuteInstructionAsync(string instructionText, bool requireConfirm, string sourceLabel)
        {
            if (!ErrorHandler.ValidateInstruction(instructionText, out string errorMessage))
            {
                AppendLog("Validation error", Color.Red);
                AppendLog(errorMessage, Color.Red);
                return;
            }

            if (isProcessing)
            {
                AppendLog("Already processing a request...", Color.Orange);
                return;
            }

            if (requireConfirm)
            {
                if (!ErrorHandler.Confirm(
                    $"Execute this instruction?\n\n\"{instructionText}\"\n\n" +
                    "This will save the command to the database and create CAD geometry.",
                    "Confirm Execution"))
                {
                    AppendLog("Execution cancelled by user", Color.Orange);
                    return;
                }
            }

            try
            {
                isProcessing = true;
                SetUIEnabled(false);

                AppendLog($"{sourceLabel ?? "Execute"}: executing instruction...", Color.Blue);
                Logger.Info($"{sourceLabel ?? "Execute"} requested: '{instructionText}'");

                if (TryExecuteReplayCommand(instructionText))
                {
                    return;
                }

                var request = new InstructionRequest(instructionText, chkUseAI.Checked);
                var response = await ApiClient.ProcessInstructionAsync(request);

                DisplayResponse(response, isPreview: false);
                AppendLog("Execution complete (saved to database)", Color.Green);

                AppendLog("", Color.Black);
                AppendLog("Creating CAD geometry...", Color.Blue);

                bool geometryCreated = ExecuteCADOperation(response, chkUseAI.Checked);

                if (geometryCreated)
                {
                    AppendLog("CAD geometry created successfully", Color.Green);
                }
                else
                {
                    AppendLog("Geometry creation skipped or failed (see details above)", Color.Orange);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = ErrorHandler.HandleException(ex, "Execute");
                AppendLog("Execution failed", Color.Red);
                AppendLog(errorMsg, Color.Red);
            }
            finally
            {
                isProcessing = false;
                SetUIEnabled(true);
            }
        }

        /// <summary>
        /// Test Units conversion utilities
        /// </summary>
        private void btnTestUnits_Click(object sender, EventArgs e)
        {
            AppendLog("\n=== Testing Units Conversion ===", Color.DarkBlue);

            double mm = 100.0;
            double m = Utils.Units.MmToM(mm);
            double backToMm = Utils.Units.MToMm(m);

            AppendLog($"100mm -> {m}m -> {backToMm}mm", Color.Black);

            if (Math.Abs(backToMm - mm) < 0.0001)
            {
                AppendLog("Units conversion test PASSED", Color.Green);
            }
            else
            {
                AppendLog("Units conversion test FAILED", Color.Red);
            }
        }

        /// <summary>
        /// Test plane selection utilities
        /// </summary>
        private void btnTestPlanes_Click(object sender, EventArgs e)
        {
            AppendLog("\n=== Testing Plane Selection ===", Color.DarkBlue);

            if (_addin == null)
            {
                AppendLog("Add-in reference not set", Color.Red);
                return;
            }

            var model = _addin.SwApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("No active document. Please open a part.", Color.Orange);
                return;
            }

            var logger = new Utils.Logger(msg => AppendLog(msg, Color.Black));

            string[] planeNames = { "Top Plane", "Front Plane", "Right Plane" };
            foreach (var planeName in planeNames)
            {
                bool found = Utils.Selection.SelectPlaneByName(_addin.SwApp, model, planeName, false, logger);
                if (found)
                {
                    AppendLog($"{planeName} selected - pausing to show...", Color.Green);
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    AppendLog($"{planeName} not found", Color.Red);
                }
            }

            AppendLog("Plane selection test complete", Color.DarkBlue);
        }

        /// <summary>
        /// Test face selection utilities
        /// </summary>
        private void btnTestFaces_Click(object sender, EventArgs e)
        {
            AppendLog("\n=== Testing Face Selection ===", Color.DarkBlue);

            if (_addin == null)
            {
                AppendLog("Add-in reference not set", Color.Red);
                return;
            }

            var model = _addin.SwApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("No active document. Please open a part with faces.", Color.Orange);
                return;
            }

            var logger = new Utils.Logger(msg => AppendLog(msg, Color.Black));

            // Test finding top planar face (by highest Y-coordinate = top/bottom plane)
            AppendLog("Searching for top-most planar face (highest Y = top)...", Color.Black);
            var topFace = Utils.Selection.GetTopMostPlanarFace(model, logger);
            if (topFace != null)
            {
                bool selected = Utils.Selection.SelectFace(model, topFace, false, logger);
                AppendLog(selected
                        ? "Top planar face (highest Y-coordinate) found and selected"
                        : "Face found but selection failed",
                        selected ? Color.Green : Color.Orange);
                AppendLog("Note: Y-axis = top/bottom, X-axis = right/left, Z-axis = front/back", Color.DarkGray);
            }
            else
            {
                AppendLog("No planar faces found (create a simple box to test)", Color.Orange);
            }

            int selCount = Utils.Selection.GetSelectionCount(model);
            AppendLog($"Current selection count: {selCount}", Color.DarkGray);
        }

        /// <summary>
        /// Test UndoScope utilities
        /// </summary>
        private void btnTestUndo_Click(object sender, EventArgs e)
        {
            AppendLog("\n=== Testing Undo Scope ===", Color.DarkBlue);

            if (_addin == null)
            {
                AppendLog("Add-in reference not set", Color.Red);
                return;
            }

            var model = _addin.SwApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("No active document. Please open a part.", Color.Orange);
                return;
            }

            var logger = new Utils.Logger(msg => AppendLog(msg, Color.Black));

            AppendLog("WARNING: EditRollback() behavior", Color.Orange);
            AppendLog("SolidWorks EditRollback() rolls back to before the SELECTED feature,", Color.DarkGray);
            AppendLog("not to a programmatic undo point. This is a SolidWorks API limitation.", Color.DarkGray);
            AppendLog("If a feature is selected, it will rollback to before that feature.\n", Color.DarkGray);

            // Test committed scope
            AppendLog("Test 1: Committed UndoScope (no rollback)", Color.DarkBlue);
            using (var scope = new Utils.UndoScope(model, "Test Operation", logger))
            {
                AppendLog("  Inside undo scope...", Color.Black);
                scope.Commit();
                AppendLog("  Scope committed - rollback prevented", Color.Black);
            }
            AppendLog("Committed scope test complete\n", Color.Green);

            // Test uncommitted scope (will attempt rollback)
            AppendLog("Test 2: Uncommitted UndoScope (triggers rollback)", Color.DarkBlue);
            AppendLog("  Deselecting all features first...", Color.DarkGray);
            model.ClearSelection2(true);

            using (var scope = new Utils.UndoScope(model, "Rollback Test", logger))
            {
                AppendLog("  Inside undo scope...", Color.Black);
                // Not calling Commit() - should trigger rollback warning
                AppendLog("  (Not committing - will attempt rollback on dispose)", Color.DarkGray);
            }
            AppendLog("Rollback scope test complete", Color.Green);
            AppendLog("\nNote: Actual rollback effect depends on feature selection state", Color.Orange);
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

        #region CAD Execution

        /// <summary>
        /// Execute the actual CAD operation based on parsed API response.
        /// This is where natural language gets converted to real geometry.
        /// Supports multi-operation instructions (multiple lines).
        /// </summary>
        /// <param name="response">Parsed instruction response from backend</param>
        /// <returns>True if all operations succeeded; false if any failed</returns>
        private bool ExecuteCADOperation(InstructionResponse response, bool useAI)
        {
            try
            {
                // Get SolidWorks application and active document
                if (_addin == null)
                {
                    AppendLog("Add-in not initialized", Color.Red);
                    Logger.Error("ExecuteCADOperation: _addin is null");
                    return false;
                }

                var swApp = _addin.SwApp;
                if (swApp == null)
                {
                    AppendLog("SolidWorks application not available", Color.Red);
                    Logger.Error("ExecuteCADOperation: SwApp is null");
                    return false;
                }

                var model = swApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
                if (model == null)
                {
                    AppendLog("No active SolidWorks document", Color.Red);
                    AppendLog("Please open a Part document first", Color.Orange);
                    System.Diagnostics.Debug.WriteLine("ExecuteCADOperation: No active document");
                    return false;
                }

                // Check document type
                if (model.GetType() != (int)SolidWorks.Interop.swconst.swDocumentTypes_e.swDocPART)
                {
                    AppendLog("Active document is not a Part", Color.Red);
                    AppendLog("Please open a Part document (not Assembly or Drawing)", Color.Orange);
                    System.Diagnostics.Debug.WriteLine($"ExecuteCADOperation: Document type is {model.GetType()}, not Part");
                    return false;
                }

                // Create logger that forwards to UI
                var logger = new Utils.Logger(msg => AppendLog(msg, Color.DarkGray));

                bool replayEnabled = ReplayLogger.EnsureSession();
                var modelInfo = BuildReplayModelInfo(model);
                if (replayEnabled)
                {
                    UpdateReplayStatusLabel();
                    string replayPath = ReplayLogger.GetCurrentReplayFilePath();
                    if (!string.IsNullOrWhiteSpace(replayPath))
                    {
                        AppendLog($"Replay log: {replayPath}", Color.DarkGray);
                    }
                }

                if (response.Operations != null)
                {
                    AppendLog($"Operations array present: {response.Operations.Count} items", Color.DarkGray);
                }
                else
                {
                    AppendLog("Operations array is null", Color.Orange);
                }

                if (response.IsMultiOperation)
                {
                    AppendLog($"Multi-operation instruction: {response.Operations.Count} operations", Color.Blue);

                    bool allSucceeded = true;
                    for (int i = 0; i < response.Operations.Count; i++)
                    {
                        var operation = response.Operations[i];
                        AppendLog($"\nOperation {i + 1}/{response.Operations.Count}:", Color.DarkBlue);

                        if (operation != null)
                        {
                            AppendLog($"  Action: {operation.Action}, Shape: {operation.ParametersData?.Shape}", Color.DarkGray);
                        }
                        else
                        {
                            AppendLog("  ERROR: Operation is null", Color.Red);
                            continue;
                        }

                        try
                        {
                            string errorMessage = null;
                            string featureBefore = GetLastFeatureName(model);
                            bool success = ExecuteSingleOperation(swApp, model, operation, logger);
                            string createdFeatureName = success ? ResolveCreatedFeatureName(featureBefore, GetLastFeatureName(model)) : null;

                            if (success)
                            {
                                AppendLog($"Operation {i + 1} completed", Color.Green);
                            }
                            else
                            {
                                AppendLog($"Operation {i + 1} failed", Color.Red);
                                allSucceeded = false;
                                errorMessage = "Operation failed - see log for details.";
                            }

                            UpdateStepExecutionState(i, success, createdFeatureName);

                            ReplayLogger.AppendEntry(new ReplayEntry
                            {
                                SchemaVersion = response.SchemaVersion ?? "1.0",
                                Instruction = response.Instruction,
                                UseAI = useAI,
                                Source = response.Source,
                                OperationIndex = i + 1,
                                OperationCount = response.Operations.Count,
                                Operation = operation,
                                Plan = response.Plan ?? new List<string>(),
                                Model = modelInfo,
                                Result = new ReplayResult { Success = success, Error = errorMessage }
                            });
                        }
                        catch (Exception opEx)
                        {
                            AppendLog($"Operation {i + 1} threw exception: {opEx.Message}", Color.Red);
                            System.Diagnostics.Debug.WriteLine($"Operation {i + 1} exception: {opEx.Message}\n{opEx.StackTrace}");
                            allSucceeded = false;

                            ReplayLogger.AppendEntry(new ReplayEntry
                            {
                                SchemaVersion = response.SchemaVersion ?? "1.0",
                                Instruction = response.Instruction,
                                UseAI = useAI,
                                Source = response.Source,
                                OperationIndex = i + 1,
                                OperationCount = response.Operations.Count,
                                Operation = operation,
                                Plan = response.Plan ?? new List<string>(),
                                Model = modelInfo,
                                Result = new ReplayResult { Success = false, Error = opEx.Message }
                            });
                        }
                    }

                    if (allSucceeded)
                    {
                        AppendLog($"\nAll {response.Operations.Count} operations completed successfully", Color.Green);
                    }
                    else
                    {
                        AppendLog("\nSome operations failed - check log above", Color.Orange);
                    }

                    return allSucceeded;
                }
                else
                {
                    // Single operation - use backward compatible path
                    var parsed = response.ParsedParameters;
                    if (parsed == null)
                    {
                        AppendLog("No parameters parsed from instruction", Color.Orange);
                        System.Diagnostics.Debug.WriteLine("ExecuteCADOperation: ParsedParameters is null");
                        return false;
                    }

                    string featureBefore = GetLastFeatureName(model);
                    bool success = ExecuteSingleOperation(swApp, model, parsed, logger);
                    string createdFeatureName = success ? ResolveCreatedFeatureName(featureBefore, GetLastFeatureName(model)) : null;

                    UpdateStepExecutionState(0, success, createdFeatureName);

                    ReplayLogger.AppendEntry(new ReplayEntry
                    {
                        SchemaVersion = response.SchemaVersion ?? "1.0",
                        Instruction = response.Instruction,
                        UseAI = useAI,
                        Source = response.Source,
                        OperationIndex = 1,
                        OperationCount = 1,
                        Operation = parsed,
                        Plan = response.Plan ?? new List<string>(),
                        Model = modelInfo,
                        Result = new ReplayResult
                        {
                            Success = success,
                            Error = success ? null : "Operation failed - see log for details."
                        }
                    });

                    return success;
                }
            }
            catch (Exception ex)
            {
                AppendLog($"CAD execution error: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"ExecuteCADOperation exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private bool TryExecuteReplayCommand(string instructionText)
        {
            if (!ReplayLogger.TryParseReplayCommand(instructionText, out string replayPath, out string error))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                AppendLog(error, Color.Red);
                return true;
            }

            AppendLog($"Replay command detected: {replayPath}", Color.Blue);

            var entries = ReplayLogger.LoadEntries(replayPath, out string loadError);
            if (!string.IsNullOrWhiteSpace(loadError))
            {
                AppendLog(loadError, Color.Red);
                return true;
            }

            if (entries.Count == 0)
            {
                AppendLog("Replay file contains no operations.", Color.Red);
                return true;
            }

            var operations = new List<ParsedParameters>();
            foreach (var entry in entries)
            {
                if (entry?.Operation != null)
                {
                    operations.Add(entry.Operation);
                }
            }

            if (operations.Count == 0)
            {
                AppendLog("Replay file has no valid operations to execute.", Color.Red);
                return true;
            }

            var firstEntry = entries[0];
            var replayResponse = new InstructionResponse
            {
                SchemaVersion = firstEntry.SchemaVersion ?? "1.0",
                Instruction = instructionText,
                Source = string.IsNullOrWhiteSpace(firstEntry.Source) ? "replay" : firstEntry.Source,
                Plan = firstEntry.Plan ?? new List<string>(),
                ParsedParameters = operations[0],
                Operations = operations
            };

            DisplayResponse(replayResponse, isPreview: false);

            AppendLog("Replaying CAD geometry...", Color.Blue);
            bool success = ExecuteCADOperation(replayResponse, useAI: false);

            if (success)
            {
                AppendLog("Replay complete", Color.Green);
            }
            else
            {
                AppendLog("Replay failed (see details above)", Color.Orange);
            }

            return true;
        }

        private ReplayModelInfo BuildReplayModelInfo(SolidWorks.Interop.sldworks.IModelDoc2 model)
        {
            string title = model?.GetTitle();
            string path = model?.GetPathName();
            if (string.IsNullOrWhiteSpace(path))
            {
                path = null;
            }

            return new ReplayModelInfo
            {
                DocumentTitle = title,
                DocumentPath = path,
                Units = "mm"
            };
        }

        /// <summary>
        /// Execute a single CAD operation by dispatching to the appropriate builder
        /// </summary>
        private bool ExecuteSingleOperation(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                string action = parsed.Action?.ToLower() ?? "";
                string shape = parsed.ParametersData?.Shape?.ToLower() ?? "";
                bool hasChamferParams = parsed.ParametersData?.ChamferDistanceMm.HasValue == true ||
                                        !string.IsNullOrWhiteSpace(parsed.ParametersData?.ChamferTarget);
                bool hasFilletParams = !string.IsNullOrWhiteSpace(parsed.ParametersData?.FilletTarget);

                AppendLog($"Action: {parsed.Action}, Shape: {parsed.ParametersData?.Shape}", Color.DarkGray);

                // Base plate
                if (shape.Contains("base") || shape.Contains("plate") || shape.Contains("rectangular") || shape.Contains("base_plate") ||
                    shape.Contains("block") || shape.Contains("box") || shape.Contains("cube"))
                {
                    AppendLog("Detected: Base plate creation", Color.Blue);
                    return CreateBasePlate(swApp, model, parsed, logger);
                }

                // Cylinder
                if (shape.Contains("cylinder") || shape.Contains("cylindrical"))
                {
                    AppendLog("Detected: Cylinder creation", Color.Blue);
                    return CreateCylinder(swApp, model, parsed, logger);
                }

                // Hole pattern
                if (shape.Contains("hole") || action.Contains("hole") || shape.Contains("pattern"))
                {
                    AppendLog("Detected: Circular hole pattern creation", Color.Blue);
                    return CreateCircularHoles(swApp, model, parsed, logger);
                }

                // Chamfer
                if (action.Contains("chamfer") || shape.Contains("chamfer") || (hasChamferParams && string.IsNullOrEmpty(shape)))
                {
                    AppendLog("Detected: Chamfer operation", Color.Blue);
                    return CreateChamfer(swApp, model, parsed, logger);
                }

                // Fillet
                if (action.Contains("fillet") || shape.Contains("fillet") || (hasFilletParams && string.IsNullOrEmpty(shape)))
                {
                    AppendLog("Detected: Fillet operation", Color.Blue);
                    return CreateFillet(swApp, model, parsed, logger);
                }

                AppendLog($"Unknown operation: {parsed.Action} / {parsed.ParametersData?.Shape}", Color.Orange);
                AppendLog("Currently supported: base plates, cylinders, circular hole patterns, fillets, chamfers", Color.Gray);
                System.Diagnostics.Debug.WriteLine($"ExecuteSingleOperation: Unrecognized action/shape: {action}/{shape}");
                return false;
            }
            catch (Exception ex)
            {
                AppendLog($"Operation error: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"ExecuteSingleOperation exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a base plate using BasePlateBuilder
        /// </summary>
        private bool CreateBasePlate(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("No parameters data for base plate", Color.Orange);
                    return false;
                }

                double widthMm = data.WidthMm ?? data.LengthMm ?? data.DiameterMm ?? 80.0;
                double lengthMm = data.LengthMm ?? data.WidthMm ?? data.DiameterMm ?? widthMm;
                double thicknessMm = data.HeightMm ?? data.DepthMm ?? 6.0;
                double? centerXmm = data.CenterXmm;
                double? centerYmm = data.CenterYmm;
                double? centerZmm = data.CenterZmm;
                bool useTopFace = data.UseTopFace ?? false;
                bool extrudeMidplane = data.ExtrudeMidplane ?? false;

                AppendLog($"Creating base plate: {lengthMm}x{widthMm}x{thicknessMm} mm", Color.Blue);

                var builder = new Builders.BasePlateBuilder(swApp, logger);
                if (useTopFace)
                {
                    return builder.CreatePlateOnTopFace(
                        model,
                        widthMm,
                        lengthMm,
                        thicknessMm,
                        centerXmm,
                        centerZmm ?? centerYmm,
                        data.DraftAngleDeg,
                        data.DraftOutward,
                        data.FlipDirection,
                        extrudeMidplane
                    );
                }

                return builder.EnsureBasePlate(
                    model,
                    widthMm,
                    thicknessMm,
                    widthMm,
                    lengthMm,
                    data.DraftAngleDeg,
                    data.DraftOutward,
                    data.FlipDirection,
                    centerXmm,
                    centerYmm,
                    extrudeMidplane
                );
            }
            catch (Exception ex)
            {
                AppendLog($"Base plate creation failed: {ex.Message}", Color.Red);
                Logger.Error($"CreateBasePlate exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a circular pattern of holes using CircularHolesBuilder
        /// </summary>
        private bool CreateCircularHoles(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("No parameters data for hole pattern", Color.Orange);
                    return false;
                }

                int count = data.Count ?? data.Pattern?.Count ?? 4;
                double diameterMm = data.DiameterMm ?? (data.RadiusMm.HasValue ? data.RadiusMm.Value * 2.0 : 5.0);

                double? angleDeg = data.Pattern?.AngleDeg ?? data.AngleDeg;
                double? patternRadiusMm = data.Pattern?.RadiusMm;
                double? depthMm = data.DepthMm ?? data.HeightMm;

                double plateSizeMm;
                if (data.WidthMm.HasValue || data.LengthMm.HasValue)
                {
                    if (data.WidthMm.HasValue && data.LengthMm.HasValue)
                        plateSizeMm = Math.Min(data.WidthMm.Value, data.LengthMm.Value);
                    else
                        plateSizeMm = data.WidthMm ?? data.LengthMm ?? 80.0;
                }
                else
                {
                    plateSizeMm = 80.0;
                    if (patternRadiusMm.HasValue)
                    {
                        double holeRadiusMm = diameterMm / 2.0;
                        double minPlateMm = 2.0 * (patternRadiusMm.Value + holeRadiusMm);
                        double paddedPlateMm = minPlateMm * 1.1;
                        if (paddedPlateMm > plateSizeMm)
                            plateSizeMm = paddedPlateMm;
                    }
                }

                AppendLog("Creating circular hole pattern:", Color.Blue);
                AppendLog($"  Count: {count}, Diameter: {diameterMm}mm", Color.DarkGray);
                if (angleDeg.HasValue)
                    AppendLog($"  Angle: {angleDeg} deg", Color.DarkGray);
                if (patternRadiusMm.HasValue)
                    AppendLog($"  Pattern radius: {patternRadiusMm}mm", Color.DarkGray);
                if (depthMm.HasValue)
                    AppendLog($"  Depth: {depthMm}mm", Color.DarkGray);

                var builder = new Builders.CircularHolesBuilder(swApp, logger);
                return builder.CreatePatternOnTopFace(
                    model,
                    count,
                    diameterMm,
                    angleDeg,
                    patternRadiusMm,
                    plateSizeMm,
                    depthMm,
                    data.DraftAngleDeg,
                    data.DraftOutward,
                    data.FlipDirection
                );
            }
            catch (Exception ex)
            {
                AppendLog($"Circular hole pattern creation failed: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"CreateCircularHoles exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create an extruded cylinder using ExtrudedCylinderBuilder
        /// </summary>
        private bool CreateCylinder(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("No parameters data for cylinder", Color.Orange);
                    return false;
                }

                double diameterMm = data.DiameterMm ?? (data.RadiusMm.HasValue ? data.RadiusMm.Value * 2.0 : 20.0);
                double heightMm = data.HeightMm ?? data.DepthMm ?? 10.0;
                double? centerXmm = data.CenterXmm;
                double? centerYmm = data.CenterYmm;
                double? centerZmm = data.CenterZmm;
                string axis = data.Axis;
                bool useTopFace = data.UseTopFace ?? false;
                bool extrudeMidplane = data.ExtrudeMidplane ?? false;

                AppendLog("Creating cylinder:", Color.Blue);
                AppendLog($"  Diameter: {diameterMm}mm, Height: {heightMm}mm", Color.DarkGray);
                if (centerXmm.HasValue || centerYmm.HasValue || centerZmm.HasValue)
                    AppendLog($"  Center: ({centerXmm ?? 0}mm, {centerYmm ?? 0}mm, {centerZmm ?? 0}mm)", Color.DarkGray);
                if (!string.IsNullOrWhiteSpace(axis))
                    AppendLog($"  Axis: {axis}", Color.DarkGray);
                if (useTopFace)
                    AppendLog("  Using top face", Color.DarkGray);
                if (extrudeMidplane)
                    AppendLog("  Midplane: true", Color.DarkGray);

                var builder = new Builders.ExtrudedCylinderBuilder(swApp, logger);
                return builder.CreateCylinderOnTopPlane(
                    model,
                    diameterMm,
                    heightMm,
                    data.DraftAngleDeg,
                    data.DraftOutward,
                    data.FlipDirection,
                    centerXmm,
                    centerYmm,
                    centerZmm,
                    axis,
                    useTopFace,
                    extrudeMidplane
                );
            }
            catch (Exception ex)
            {
                AppendLog($"Cylinder creation failed: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"CreateCylinder exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a fillet using FilletBuilder
        /// </summary>
        private bool CreateFillet(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("No parameters data for fillet", Color.Orange);
                    return false;
                }

                double? radiusMm = data.RadiusMm ?? data.DiameterMm;
                if (!radiusMm.HasValue || radiusMm.Value <= 0)
                {
                    AppendLog("Fillet requires a positive radius in mm (e.g., 'fillet radius 2 mm')", Color.Red);
                    return false;
                }

                string target = data.FilletTarget?.Trim().ToLowerInvariant();
                bool allEdges = target == "all_edges";

                AppendLog("Creating fillet:", Color.Blue);
                AppendLog($"  Radius: {radiusMm.Value}mm", Color.DarkGray);
                AppendLog($"  Target: {(allEdges ? "All edges" : "Recent feature edges")}", Color.DarkGray);

                var builder = new Builders.FilletBuilder(swApp, logger);

                if (allEdges)
                {
                    return builder.ApplyFilletToAllSharpEdges(model, radiusMm.Value);
                }
                else
                {
                    return builder.ApplyFilletToRecentEdges(model, radiusMm.Value);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Fillet creation failed: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"CreateFillet exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Create a chamfer using ChamferBuilder
        /// </summary>
        private bool CreateChamfer(
            SolidWorks.Interop.sldworks.ISldWorks swApp,
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            ParsedParameters parsed,
            Interfaces.ILogger logger)
        {
            try
            {
                var data = parsed.ParametersData;
                if (data == null)
                {
                    AppendLog("No parameters data for chamfer", Color.Orange);
                    return false;
                }

                double? distanceMm = data.ChamferDistanceMm;
                if (!distanceMm.HasValue || distanceMm.Value <= 0)
                {
                    AppendLog("Chamfer requires a positive distance in mm (e.g., 'chamfer 2 mm at 45 deg')", Color.Red);
                    return false;
                }

                double? angleDeg = data.AngleDeg;
                if (angleDeg.HasValue && (angleDeg.Value <= 0 || angleDeg.Value >= 180))
                {
                    AppendLog($"Invalid chamfer angle: {angleDeg.Value} deg (must be between 0 and 180)", Color.Red);
                    return false;
                }

                string target = data.ChamferTarget?.Trim().ToLowerInvariant();
                bool allEdges = target == "all_edges";

                AppendLog("Creating chamfer:", Color.Blue);
                AppendLog($"  Distance: {distanceMm.Value}mm", Color.DarkGray);
                if (angleDeg.HasValue)
                    AppendLog($"  Angle: {angleDeg.Value} deg", Color.DarkGray);
                AppendLog($"  Target: {(allEdges ? "All edges" : "Recent feature edges")}", Color.DarkGray);

                var builder = new Builders.ChamferBuilder(swApp, logger);

                if (allEdges)
                {
                    return builder.ApplyChamferToAllSharpEdges(model, distanceMm.Value, angleDeg);
                }
                else
                {
                    return builder.ApplyChamferToRecentEdges(model, distanceMm.Value, angleDeg);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Chamfer creation failed: {ex.Message}", Color.Red);
                System.Diagnostics.Debug.WriteLine($"CreateChamfer exception: {ex.Message}");
                return false;
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

            AppendLog("============================================", Color.DarkBlue);
            AppendLog($"  {mode} RESULTS", Color.DarkBlue);
            AppendLog("============================================", Color.DarkBlue);
            AppendLog($"Source: {source} parsing", Color.DarkGray);
            AppendLog($"Schema Version: {response.SchemaVersion}", Color.DarkGray);
            AppendLog("", Color.Black);

            // Display plan
            AppendLog("PLAN:", Color.DarkBlue);
            txtPlan.Clear();
            if (response.Plan != null && response.Plan.Count > 0)
            {
                foreach (var step in response.Plan)
                {
                    AppendLog($"  - {step}", Color.Black);
                    txtPlan.AppendText($"- {step}\r\n");
                }
            }
            else
            {
                AppendLog("  (No plan available)", Color.Gray);
            }

            AppendLog("", Color.Black);

            // Display parsed parameters
            if (response.ParsedParameters != null)
            {
                AppendLog("PARSED PARAMETERS:", Color.DarkBlue);
                AppendLog($"  Action: {response.ParsedParameters.GetActionDescription()}", Color.Black);
                AppendLog($"  {response.ParsedParameters.GetParametersSummary()}", Color.Black);
            }

            // Display operations summary if multiple
            if (response.Operations != null && response.Operations.Count > 1)
            {
                AppendLog("", Color.Black);
                AppendLog("OPERATIONS:", Color.DarkBlue);
                for (int i = 0; i < response.Operations.Count; i++)
                {
                    var op = response.Operations[i];
                    AppendLog($"  {i + 1}. {op.GetActionDescription()} - {op.GetParametersSummary()}", Color.Black);
                }
            }

            AppendLog("============================================\n", Color.DarkBlue);

            _lastResponse = response;
            _lastUseAI = response.IsAIParsed;
            LoadStepsFromResponse(response);

            // Update status
            lblStatus.Text = $"{mode} Complete - {source}";
            lblStatus.ForeColor = Color.Green;
        }

        private void DisplayPlannerResponse(PlannerResponse response)
        {
            if (response == null)
            {
                AppendLog("Planner response was empty", Color.Orange);
                return;
            }

            _plannerResponse = response;
            _plannerStateId = response.StateId;

            string stateShort = FormatPlannerStateId(_plannerStateId);
            string status = response.Status ?? "unknown";
            string plannerLabel = $"Planner: {status}";
            if (!string.IsNullOrWhiteSpace(stateShort))
            {
                plannerLabel += $" (state {stateShort})";
            }

            if (lblPlannerState != null)
            {
                lblPlannerState.Text = plannerLabel;
            }

            txtPlan.Clear();
            if (response.Plan != null && response.Plan.Count > 0)
            {
                foreach (var step in response.Plan)
                {
                    txtPlan.AppendText($"- {step}\r\n");
                }
            }
            else
            {
                txtPlan.Text = "(No plan available)";
            }

            if (txtPlannerQuestions != null)
            {
                txtPlannerQuestions.Clear();
                if (response.Questions != null && response.Questions.Count > 0)
                {
                    foreach (var question in response.Questions)
                    {
                        txtPlannerQuestions.AppendText($"{question.Id}: {question.Prompt}\r\n");
                    }
                }
                else
                {
                    txtPlannerQuestions.Text = "No open questions.";
                }
            }

            if (txtPlannerAnswers != null)
            {
                if (response.Questions != null && response.Questions.Count > 0)
                {
                    txtPlannerAnswers.Text = string.Join(
                        "\r\n",
                        response.Questions.Select(q => $"{q.Id}="));
                }
                else
                {
                    txtPlannerAnswers.Clear();
                }
            }

            if (response.Notes != null && response.Notes.Count > 0)
            {
                AppendLog("Planner notes:", Color.DarkGray);
                foreach (var note in response.Notes)
                {
                    AppendLog($"  - {note}", Color.DarkGray);
                }
            }

            if (response.Operations != null && response.Operations.Count > 0)
            {
                var plannerInstructionResponse = new InstructionResponse
                {
                    SchemaVersion = response.SchemaVersion,
                    Instruction = response.Instruction,
                    Source = "planner",
                    Plan = response.Plan,
                    ParsedParameters = response.Operations[0],
                    Operations = response.Operations
                };

                _lastResponse = plannerInstructionResponse;
                _lastUseAI = false;
                LoadStepsFromResponse(plannerInstructionResponse);
            }
            else
            {
                if (lstSteps != null)
                {
                    lstSteps.Items.Clear();
                    lstSteps.Items.Add("(Planner awaiting answers)", false);
                    lstSteps.Enabled = false;
                }
            }

            lblStatus.Text = $"Planner: {status}";
            lblStatus.ForeColor = status == "ready" ? Color.Green : Color.DarkOrange;
        }

        private Dictionary<string, object> ParsePlannerAnswers(string answerText)
        {
            var results = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(answerText))
            {
                return results;
            }

            string normalized = answerText.Replace("\r", "\n");
            string[] entries = normalized.Split(new[] { '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string entry in entries)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                string[] parts = trimmed.Split(new[] { '=', ':' }, 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                results[key] = value;
            }

            return results;
        }

        private string FormatPlannerStateId(string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId))
            {
                return null;
            }

            return stateId.Length > 8 ? stateId.Substring(0, 8) : stateId;
        }

        private void LoadStepsFromResponse(InstructionResponse response)
        {
            if (lstSteps == null)
            {
                return;
            }

            lstSteps.Items.Clear();
            _stepOperations.Clear();
            _executedSteps.Clear();
            _undoStack.Clear();
            _stepFeatureNames.Clear();

            if (response == null)
            {
                lstSteps.Items.Add("(No steps available)", false);
                lstSteps.Enabled = false;
                return;
            }

            var operations = new List<ParsedParameters>();
            if (response.Operations != null && response.Operations.Count > 0)
            {
                operations.AddRange(response.Operations);
            }
            else if (response.ParsedParameters != null)
            {
                operations.Add(response.ParsedParameters);
            }

            if (operations.Count == 0)
            {
                lstSteps.Items.Add("(No steps available)", false);
                lstSteps.Enabled = false;
                return;
            }

            lstSteps.Enabled = true;
            for (int i = 0; i < operations.Count; i++)
            {
                var op = operations[i];
                _stepOperations.Add(op);
                lstSteps.Items.Add(BuildStepLabel(i, op, false), true);
            }
        }

        private string BuildStepLabel(int index, ParsedParameters operation, bool executed)
        {
            string prefix = executed ? "✓ " : "";
            string description = operation != null
                ? $"{operation.GetActionDescription()} - {operation.GetParametersSummary()}"
                : "Unknown step";

            return $"{prefix}{index + 1}. {description}";
        }

        private void UpdateStepExecutionState(int index, bool success, string createdFeatureName)
        {
            if (!success)
            {
                return;
            }

            _executedSteps.Add(index);
            _undoStack.Push(index);

            if (!string.IsNullOrWhiteSpace(createdFeatureName))
            {
                _stepFeatureNames[index] = createdFeatureName;
            }
            else
            {
                _stepFeatureNames.Remove(index);
            }

            if (lstSteps != null && index >= 0 && index < _stepOperations.Count && index < lstSteps.Items.Count)
            {
                lstSteps.Items[index] = BuildStepLabel(index, _stepOperations[index], true);
            }
        }

        private string ResolveCreatedFeatureName(string beforeName, string afterName)
        {
            if (string.IsNullOrWhiteSpace(afterName))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(beforeName) &&
                string.Equals(beforeName, afterName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return afterName;
        }

        private string GetLastFeatureName(SolidWorks.Interop.sldworks.IModelDoc2 model)
        {
            if (model == null)
            {
                return null;
            }

            try
            {
                SolidWorks.Interop.sldworks.IFeature feature =
                    model.FirstFeature() as SolidWorks.Interop.sldworks.IFeature;
                SolidWorks.Interop.sldworks.IFeature lastFeature = null;

                while (feature != null)
                {
                    string typeName = feature.GetTypeName2() ?? string.Empty;
                    bool isReference =
                        typeName == "ProfileFeature" ||
                        typeName == "RefPlane" ||
                        typeName == "RefAxis" ||
                        typeName == "RefPoint" ||
                        typeName == "CoordSys" ||
                        typeName.Contains("OriginFeature");

                    if (!isReference)
                    {
                        lastFeature = feature;
                    }

                    feature = feature.GetNextFeature() as SolidWorks.Interop.sldworks.IFeature;
                }

                return lastFeature?.Name;
            }
            catch
            {
                return null;
            }
        }

        private void ExecuteStepIndices(IList<int> indices)
        {
            if (_stepOperations.Count == 0 || _lastResponse == null)
            {
                AppendLog("No steps loaded. Preview or Execute an instruction first.", Color.Orange);
                return;
            }

            if (!TryGetActiveModel(out var swApp, out var model, out var logger))
            {
                return;
            }

            bool replayEnabled = ReplayLogger.EnsureSession();
            var modelInfo = BuildReplayModelInfo(model);
            if (replayEnabled)
            {
                UpdateReplayStatusLabel();
            }

            bool allSucceeded = true;
            foreach (int index in indices)
            {
                if (index < 0 || index >= _stepOperations.Count)
                {
                    continue;
                }

                if (_executedSteps.Contains(index))
                {
                    AppendLog($"Step {index + 1} already executed - skipping", Color.DarkGray);
                    continue;
                }

                var operation = _stepOperations[index];
                if (operation == null)
                {
                    AppendLog($"Step {index + 1} is empty - skipping", Color.Orange);
                    continue;
                }
                AppendLog($"\nRunning step {index + 1}/{_stepOperations.Count}:", Color.DarkBlue);
                AppendLog($"  {operation.GetActionDescription()} - {operation.GetParametersSummary()}", Color.DarkGray);

                string featureBefore = GetLastFeatureName(model);
                bool success = ExecuteSingleOperation(swApp, model, operation, logger);
                string createdFeatureName = success ? ResolveCreatedFeatureName(featureBefore, GetLastFeatureName(model)) : null;
                if (success)
                {
                    AppendLog($"Step {index + 1} completed", Color.Green);
                }
                else
                {
                    AppendLog($"Step {index + 1} failed", Color.Red);
                    allSucceeded = false;
                }

                UpdateStepExecutionState(index, success, createdFeatureName);

                ReplayLogger.AppendEntry(new ReplayEntry
                {
                    SchemaVersion = _lastResponse.SchemaVersion ?? "1.0",
                    Instruction = _lastResponse.Instruction,
                    UseAI = _lastUseAI,
                    Source = _lastResponse.Source,
                    OperationIndex = index + 1,
                    OperationCount = _stepOperations.Count,
                    Operation = operation,
                    Plan = _lastResponse.Plan ?? new List<string>(),
                    Model = modelInfo,
                    Result = new ReplayResult
                    {
                        Success = success,
                        Error = success ? null : "Step failed - see log for details."
                    }
                });
            }

            if (allSucceeded)
            {
                AppendLog("\nSelected steps completed successfully", Color.Green);
            }
            else
            {
                AppendLog("\nSome selected steps failed - check log above", Color.Orange);
            }
        }

        private bool TryGetActiveModel(
            out SolidWorks.Interop.sldworks.ISldWorks swApp,
            out SolidWorks.Interop.sldworks.IModelDoc2 model,
            out Utils.Logger logger)
        {
            swApp = _addin?.SwApp;
            model = null;
            logger = null;

            if (swApp == null)
            {
                AppendLog("SolidWorks application not available", Color.Red);
                return false;
            }

            model = swApp.ActiveDoc as SolidWorks.Interop.sldworks.IModelDoc2;
            if (model == null)
            {
                AppendLog("No active SolidWorks document", Color.Red);
                AppendLog("Please open a Part document first", Color.Orange);
                return false;
            }

            if (model.GetType() != (int)SolidWorks.Interop.swconst.swDocumentTypes_e.swDocPART)
            {
                AppendLog("Active document is not a Part", Color.Red);
                AppendLog("Please open a Part document (not Assembly or Drawing)", Color.Orange);
                return false;
            }

            logger = new Utils.Logger(msg => AppendLog(msg, Color.DarkGray));
            return true;
        }

        private bool TryUndoLastOperation(
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            Utils.Logger logger,
            int stepIndex)
        {
            if (model == null)
            {
                return false;
            }

            if (_stepFeatureNames.TryGetValue(stepIndex, out string featureName) &&
                !string.IsNullOrWhiteSpace(featureName))
            {
                AppendLog($"Undo: deleting feature '{featureName}'", Color.DarkGray);
                if (TryDeleteFeature(model, featureName, logger))
                {
                    return true;
                }

                AppendLog($"Undo: failed to delete feature '{featureName}', trying SolidWorks undo.", Color.Orange);
            }

            return TryEditUndo(model, logger);
        }

        private bool TryDeleteFeature(
            SolidWorks.Interop.sldworks.IModelDoc2 model,
            string featureName,
            Utils.Logger logger)
        {
            try
            {
                model.ClearSelection2(true);
                bool selected = model.Extension.SelectByID2(
                    featureName,
                    "BODYFEATURE",
                    0,
                    0,
                    0,
                    false,
                    0,
                    null,
                    (int)SolidWorks.Interop.swconst.swSelectOption_e.swSelectOptionDefault);

                if (!selected)
                {
                    AppendLog($"Undo: could not select feature '{featureName}'", Color.Orange);
                    logger?.Warn($"Failed to select feature '{featureName}' for delete");
                    return false;
                }

                model.EditDelete();
                model.ClearSelection2(true);
                model.ForceRebuild3(false);
                logger?.Info($"Deleted feature '{featureName}'");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"Undo: exception deleting feature '{featureName}': {ex.Message}", Color.Red);
                logger?.Error($"Exception deleting feature '{featureName}': {ex.Message}");
                return false;
            }
        }

        private bool TryEditUndo(SolidWorks.Interop.sldworks.IModelDoc2 model, Utils.Logger logger)
        {
            try
            {
                dynamic dynModel = model;
                bool result = dynModel.EditUndo2(1);
                if (!result)
                {
                    logger?.Warn("EditUndo2 returned false");
                }
                return result;
            }
            catch (Exception ex)
            {
                logger?.Warn($"EditUndo2 threw exception: {ex.Message}");
                try
                {
                    dynamic dynModel = model;
                    bool result = dynModel.EditUndo();
                    if (!result)
                    {
                        logger?.Warn("EditUndo returned false");
                    }
                    return result;
                }
                catch (Exception ex2)
                {
                    logger?.Error($"EditUndo failed: {ex2.Message}");
                    return false;
                }
            }
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
            btnPlan.Enabled = enabled;
            btnUpdateUrl.Enabled = enabled;
            btnTestConnection.Enabled = enabled;
            btnReplayStart.Enabled = enabled;
            btnReplayPause.Enabled = enabled;
            btnReplayResume.Enabled = enabled;
            btnReplayEnd.Enabled = enabled;
            btnReplayLast.Enabled = enabled;
            btnOpenReplay.Enabled = enabled;
            btnPlannerSubmit.Enabled = enabled;
            btnRunSelectedStep.Enabled = enabled;
            btnRunCheckedSteps.Enabled = enabled;
            btnRunNextStep.Enabled = enabled;
            btnUndoLastStep.Enabled = enabled;
            btnVoiceRecord.Enabled = enabled;
            btnVoiceConfirm.Enabled = enabled && !string.IsNullOrWhiteSpace(_lastVoiceTranscript);
            btnVoiceCancel.Enabled = enabled;
            if (chkUseWhisper != null)
            {
                chkUseWhisper.Enabled = enabled;
            }
            if (lstSteps != null)
            {
                lstSteps.Enabled = enabled;
            }
            if (txtPlannerAnswers != null)
            {
                txtPlannerAnswers.Enabled = enabled;
            }
            txtInstruction.Enabled = enabled;
            txtApiBase.Enabled = enabled;
            chkUseAI.Enabled = enabled;

            if (!enabled)
            {
                lblStatus.Text = "Processing...";
                lblStatus.ForeColor = Color.Orange;
                StopVoiceCapture(transcribe: false);
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
                lblConnectionStatus.Text = "Connected";
                lblConnectionStatus.ForeColor = Color.Green;
            }
            else
            {
                lblConnectionStatus.Text = "Disconnected";
                lblConnectionStatus.ForeColor = Color.Red;
            }
        }

        #endregion
    }
}
