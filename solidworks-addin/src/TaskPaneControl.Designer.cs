namespace TextToCad.SolidWorksAddin
{
    partial class TaskPaneControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblInstruction = new System.Windows.Forms.Label();
            this.txtInstruction = new System.Windows.Forms.TextBox();
            this.chkUseAI = new System.Windows.Forms.CheckBox();
            this.btnPlan = new System.Windows.Forms.Button();
            this.btnPreview = new System.Windows.Forms.Button();
            this.btnExecute = new System.Windows.Forms.Button();
            this.grpPlan = new System.Windows.Forms.GroupBox();
            this.txtPlan = new System.Windows.Forms.TextBox();
            this.grpPlanner = new System.Windows.Forms.GroupBox();
            this.lblPlannerState = new System.Windows.Forms.Label();
            this.txtPlannerQuestions = new System.Windows.Forms.TextBox();
            this.txtPlannerAnswers = new System.Windows.Forms.TextBox();
            this.btnPlannerSubmit = new System.Windows.Forms.Button();
            this.grpSteps = new System.Windows.Forms.GroupBox();
            this.lstSteps = new System.Windows.Forms.CheckedListBox();
            this.btnRunSelectedStep = new System.Windows.Forms.Button();
            this.btnRunCheckedSteps = new System.Windows.Forms.Button();
            this.btnRunNextStep = new System.Windows.Forms.Button();
            this.btnUndoLastStep = new System.Windows.Forms.Button();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.grpSettings = new System.Windows.Forms.GroupBox();
            this.btnOpenLogs = new System.Windows.Forms.Button();
            this.btnReplayLast = new System.Windows.Forms.Button();
            this.btnOpenReplay = new System.Windows.Forms.Button();
            this.btnReplayStart = new System.Windows.Forms.Button();
            this.btnReplayPause = new System.Windows.Forms.Button();
            this.btnReplayResume = new System.Windows.Forms.Button();
            this.btnReplayEnd = new System.Windows.Forms.Button();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.lblReplayStatus = new System.Windows.Forms.Label();
            this.btnUpdateUrl = new System.Windows.Forms.Button();
            this.lblApiBase = new System.Windows.Forms.Label();
            this.txtApiBase = new System.Windows.Forms.TextBox();
            this.grpTestUtils = new System.Windows.Forms.GroupBox();
            this.btnTestUnits = new System.Windows.Forms.Button();
            this.btnTestPlanes = new System.Windows.Forms.Button();
            this.btnTestFaces = new System.Windows.Forms.Button();
            this.btnTestUndo = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.grpPlan.SuspendLayout();
            this.grpPlanner.SuspendLayout();
            this.grpSteps.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.grpSettings.SuspendLayout();
            this.grpTestUtils.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(10, 10, 10, 5);
            this.lblTitle.Size = new System.Drawing.Size(350, 40);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Text-to-CAD";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblVersion.Location = new System.Drawing.Point(10, 42);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(132, 13);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Current test version 3.0";
            // 
            // lblInstruction
            // 
            this.lblInstruction.AutoSize = true;
            this.lblInstruction.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInstruction.Location = new System.Drawing.Point(10, 60);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(99, 15);
            this.lblInstruction.TabIndex = 2;
            this.lblInstruction.Text = "CAD Instruction:";
            // 
            // txtInstruction
            // 
            this.txtInstruction.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtInstruction.Location = new System.Drawing.Point(10, 80);
            this.txtInstruction.Multiline = true;
            this.txtInstruction.Name = "txtInstruction";
            this.txtInstruction.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtInstruction.Size = new System.Drawing.Size(330, 60);
            this.txtInstruction.TabIndex = 3;
            this.txtInstruction.Enter += new System.EventHandler(this.txtInstruction_Enter);
            this.txtInstruction.Leave += new System.EventHandler(this.txtInstruction_Leave);
            // 
            // chkUseAI
            // 
            this.chkUseAI.AutoSize = true;
            this.chkUseAI.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkUseAI.Location = new System.Drawing.Point(10, 150);
            this.chkUseAI.Name = "chkUseAI";
            this.chkUseAI.Size = new System.Drawing.Size(218, 19);
            this.chkUseAI.TabIndex = 4;
            this.chkUseAI.Text = "Use AI parsing (requires API key)";
            this.chkUseAI.UseVisualStyleBackColor = true;
            // 
            // btnPlan
            // 
            this.btnPlan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(108)))), ((int)(((byte)(117)))), ((int)(((byte)(125)))));
            this.btnPlan.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPlan.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnPlan.ForeColor = System.Drawing.Color.White;
            this.btnPlan.Location = new System.Drawing.Point(10, 180);
            this.btnPlan.Name = "btnPlan";
            this.btnPlan.Size = new System.Drawing.Size(100, 35);
            this.btnPlan.TabIndex = 5;
            this.btnPlan.Text = "Plan";
            this.btnPlan.UseVisualStyleBackColor = false;
            this.btnPlan.Click += new System.EventHandler(this.btnPlan_Click);
            // 
            // btnPreview
            // 
            this.btnPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnPreview.ForeColor = System.Drawing.Color.White;
            this.btnPreview.Location = new System.Drawing.Point(120, 180);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(100, 35);
            this.btnPreview.TabIndex = 6;
            this.btnPreview.Text = "Preview";
            this.btnPreview.UseVisualStyleBackColor = false;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // btnExecute
            // 
            this.btnExecute.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this.btnExecute.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExecute.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExecute.ForeColor = System.Drawing.Color.White;
            this.btnExecute.Location = new System.Drawing.Point(230, 180);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(100, 35);
            this.btnExecute.TabIndex = 7;
            this.btnExecute.Text = "Execute";
            this.btnExecute.UseVisualStyleBackColor = false;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // grpPlan
            // 
            this.grpPlan.Controls.Add(this.txtPlan);
            this.grpPlan.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpPlan.Location = new System.Drawing.Point(10, 225);
            this.grpPlan.Name = "grpPlan";
            this.grpPlan.Size = new System.Drawing.Size(330, 120);
            this.grpPlan.TabIndex = 7;
            this.grpPlan.TabStop = false;
            this.grpPlan.Text = "Execution Plan";
            // 
            // txtPlan
            // 
            this.txtPlan.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(250)))));
            this.txtPlan.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPlan.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtPlan.Location = new System.Drawing.Point(3, 19);
            this.txtPlan.Multiline = true;
            this.txtPlan.Name = "txtPlan";
            this.txtPlan.ReadOnly = true;
            this.txtPlan.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPlan.Size = new System.Drawing.Size(324, 98);
            this.txtPlan.TabIndex = 0;
            // 
            // grpPlanner
            // 
            this.grpPlanner.Controls.Add(this.btnPlannerSubmit);
            this.grpPlanner.Controls.Add(this.txtPlannerAnswers);
            this.grpPlanner.Controls.Add(this.txtPlannerQuestions);
            this.grpPlanner.Controls.Add(this.lblPlannerState);
            this.grpPlanner.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpPlanner.Location = new System.Drawing.Point(10, 355);
            this.grpPlanner.Name = "grpPlanner";
            this.grpPlanner.Size = new System.Drawing.Size(330, 150);
            this.grpPlanner.TabIndex = 8;
            this.grpPlanner.TabStop = false;
            this.grpPlanner.Text = "Planner";
            // 
            // lblPlannerState
            // 
            this.lblPlannerState.AutoSize = true;
            this.lblPlannerState.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblPlannerState.Location = new System.Drawing.Point(6, 20);
            this.lblPlannerState.Name = "lblPlannerState";
            this.lblPlannerState.Size = new System.Drawing.Size(83, 13);
            this.lblPlannerState.TabIndex = 0;
            this.lblPlannerState.Text = "Planner: idle";
            // 
            // txtPlannerQuestions
            // 
            this.txtPlannerQuestions.BackColor = System.Drawing.Color.White;
            this.txtPlannerQuestions.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.txtPlannerQuestions.Location = new System.Drawing.Point(6, 38);
            this.txtPlannerQuestions.Multiline = true;
            this.txtPlannerQuestions.Name = "txtPlannerQuestions";
            this.txtPlannerQuestions.ReadOnly = true;
            this.txtPlannerQuestions.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPlannerQuestions.Size = new System.Drawing.Size(318, 50);
            this.txtPlannerQuestions.TabIndex = 1;
            // 
            // txtPlannerAnswers
            // 
            this.txtPlannerAnswers.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.txtPlannerAnswers.Location = new System.Drawing.Point(6, 92);
            this.txtPlannerAnswers.Multiline = true;
            this.txtPlannerAnswers.Name = "txtPlannerAnswers";
            this.txtPlannerAnswers.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPlannerAnswers.Size = new System.Drawing.Size(318, 30);
            this.txtPlannerAnswers.TabIndex = 2;
            // 
            // btnPlannerSubmit
            // 
            this.btnPlannerSubmit.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnPlannerSubmit.Location = new System.Drawing.Point(6, 124);
            this.btnPlannerSubmit.Name = "btnPlannerSubmit";
            this.btnPlannerSubmit.Size = new System.Drawing.Size(318, 20);
            this.btnPlannerSubmit.TabIndex = 3;
            this.btnPlannerSubmit.Text = "Submit Answers";
            this.btnPlannerSubmit.UseVisualStyleBackColor = true;
            this.btnPlannerSubmit.Click += new System.EventHandler(this.btnPlannerSubmit_Click);
            // 
            // grpSteps
            // 
            this.grpSteps.Controls.Add(this.lstSteps);
            this.grpSteps.Controls.Add(this.btnRunSelectedStep);
            this.grpSteps.Controls.Add(this.btnRunCheckedSteps);
            this.grpSteps.Controls.Add(this.btnRunNextStep);
            this.grpSteps.Controls.Add(this.btnUndoLastStep);
            this.grpSteps.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpSteps.Location = new System.Drawing.Point(10, 515);
            this.grpSteps.Name = "grpSteps";
            this.grpSteps.Size = new System.Drawing.Size(330, 140);
            this.grpSteps.TabIndex = 9;
            this.grpSteps.TabStop = false;
            this.grpSteps.Text = "Steps";
            // 
            // lstSteps
            // 
            this.lstSteps.CheckOnClick = true;
            this.lstSteps.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lstSteps.FormattingEnabled = true;
            this.lstSteps.Location = new System.Drawing.Point(6, 20);
            this.lstSteps.Name = "lstSteps";
            this.lstSteps.Size = new System.Drawing.Size(318, 79);
            this.lstSteps.TabIndex = 0;
            // 
            // btnRunSelectedStep
            // 
            this.btnRunSelectedStep.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnRunSelectedStep.Location = new System.Drawing.Point(6, 105);
            this.btnRunSelectedStep.Name = "btnRunSelectedStep";
            this.btnRunSelectedStep.Size = new System.Drawing.Size(76, 25);
            this.btnRunSelectedStep.TabIndex = 1;
            this.btnRunSelectedStep.Text = "Run Selected";
            this.btnRunSelectedStep.UseVisualStyleBackColor = true;
            this.btnRunSelectedStep.Click += new System.EventHandler(this.btnRunSelectedStep_Click);
            // 
            // btnRunCheckedSteps
            // 
            this.btnRunCheckedSteps.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnRunCheckedSteps.Location = new System.Drawing.Point(88, 105);
            this.btnRunCheckedSteps.Name = "btnRunCheckedSteps";
            this.btnRunCheckedSteps.Size = new System.Drawing.Size(76, 25);
            this.btnRunCheckedSteps.TabIndex = 2;
            this.btnRunCheckedSteps.Text = "Run Checked";
            this.btnRunCheckedSteps.UseVisualStyleBackColor = true;
            this.btnRunCheckedSteps.Click += new System.EventHandler(this.btnRunCheckedSteps_Click);
            // 
            // btnRunNextStep
            // 
            this.btnRunNextStep.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnRunNextStep.Location = new System.Drawing.Point(170, 105);
            this.btnRunNextStep.Name = "btnRunNextStep";
            this.btnRunNextStep.Size = new System.Drawing.Size(76, 25);
            this.btnRunNextStep.TabIndex = 3;
            this.btnRunNextStep.Text = "Run Next";
            this.btnRunNextStep.UseVisualStyleBackColor = true;
            this.btnRunNextStep.Click += new System.EventHandler(this.btnRunNextStep_Click);
            // 
            // btnUndoLastStep
            // 
            this.btnUndoLastStep.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnUndoLastStep.Location = new System.Drawing.Point(252, 105);
            this.btnUndoLastStep.Name = "btnUndoLastStep";
            this.btnUndoLastStep.Size = new System.Drawing.Size(76, 25);
            this.btnUndoLastStep.TabIndex = 4;
            this.btnUndoLastStep.Text = "Undo Last";
            this.btnUndoLastStep.UseVisualStyleBackColor = true;
            this.btnUndoLastStep.Click += new System.EventHandler(this.btnUndoLastStep_Click);
            // 
            // grpLog
            // 
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Controls.Add(this.btnClearLog);
            this.grpLog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpLog.Location = new System.Drawing.Point(10, 665);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(330, 200);
            this.grpLog.TabIndex = 10;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "Log";
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.Color.White;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 8.25F);
            this.txtLog.Location = new System.Drawing.Point(6, 19);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(318, 145);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            // 
            // btnClearLog
            // 
            this.btnClearLog.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnClearLog.Location = new System.Drawing.Point(6, 170);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(318, 24);
            this.btnClearLog.TabIndex = 1;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // grpSettings
            // 
            this.grpSettings.Controls.Add(this.btnReplayEnd);
            this.grpSettings.Controls.Add(this.btnReplayResume);
            this.grpSettings.Controls.Add(this.btnReplayPause);
            this.grpSettings.Controls.Add(this.btnReplayStart);
            this.grpSettings.Controls.Add(this.btnOpenReplay);
            this.grpSettings.Controls.Add(this.btnReplayLast);
            this.grpSettings.Controls.Add(this.btnOpenLogs);
            this.grpSettings.Controls.Add(this.btnTestConnection);
            this.grpSettings.Controls.Add(this.lblConnectionStatus);
            this.grpSettings.Controls.Add(this.lblReplayStatus);
            this.grpSettings.Controls.Add(this.btnUpdateUrl);
            this.grpSettings.Controls.Add(this.lblApiBase);
            this.grpSettings.Controls.Add(this.txtApiBase);
            this.grpSettings.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpSettings.Location = new System.Drawing.Point(10, 875);
            this.grpSettings.Name = "grpSettings";
            this.grpSettings.Size = new System.Drawing.Size(330, 250);
            this.grpSettings.TabIndex = 11;
            this.grpSettings.TabStop = false;
            this.grpSettings.Text = "Settings";
            // 
            // grpTestUtils
            // 
            this.grpTestUtils.Controls.Add(this.btnTestUnits);
            this.grpTestUtils.Controls.Add(this.btnTestPlanes);
            this.grpTestUtils.Controls.Add(this.btnTestFaces);
            this.grpTestUtils.Controls.Add(this.btnTestUndo);
            this.grpTestUtils.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpTestUtils.Location = new System.Drawing.Point(10, 1130);
            this.grpTestUtils.Name = "grpTestUtils";
            this.grpTestUtils.Size = new System.Drawing.Size(330, 100);
            this.grpTestUtils.TabIndex = 13;
            this.grpTestUtils.TabStop = false;
            this.grpTestUtils.Text = "Test Utilities (SW-2)";
            // 
            // btnTestUnits
            // 
            this.btnTestUnits.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTestUnits.Location = new System.Drawing.Point(6, 25);
            this.btnTestUnits.Name = "btnTestUnits";
            this.btnTestUnits.Size = new System.Drawing.Size(154, 30);
            this.btnTestUnits.TabIndex = 0;
            this.btnTestUnits.Text = "Test Units";
            this.btnTestUnits.UseVisualStyleBackColor = true;
            this.btnTestUnits.Click += new System.EventHandler(this.btnTestUnits_Click);
            // 
            // btnTestPlanes
            // 
            this.btnTestPlanes.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTestPlanes.Location = new System.Drawing.Point(170, 25);
            this.btnTestPlanes.Name = "btnTestPlanes";
            this.btnTestPlanes.Size = new System.Drawing.Size(154, 30);
            this.btnTestPlanes.TabIndex = 1;
            this.btnTestPlanes.Text = "Test Plane Selection";
            this.btnTestPlanes.UseVisualStyleBackColor = true;
            this.btnTestPlanes.Click += new System.EventHandler(this.btnTestPlanes_Click);
            // 
            // btnTestFaces
            // 
            this.btnTestFaces.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTestFaces.Location = new System.Drawing.Point(6, 61);
            this.btnTestFaces.Name = "btnTestFaces";
            this.btnTestFaces.Size = new System.Drawing.Size(154, 30);
            this.btnTestFaces.TabIndex = 2;
            this.btnTestFaces.Text = "Test Face Selection";
            this.btnTestFaces.UseVisualStyleBackColor = true;
            this.btnTestFaces.Click += new System.EventHandler(this.btnTestFaces_Click);
            // 
            // btnTestUndo
            // 
            this.btnTestUndo.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTestUndo.Location = new System.Drawing.Point(170, 61);
            this.btnTestUndo.Name = "btnTestUndo";
            this.btnTestUndo.Size = new System.Drawing.Size(154, 30);
            this.btnTestUndo.TabIndex = 3;
            this.btnTestUndo.Text = "Test Undo Scope";
            this.btnTestUndo.UseVisualStyleBackColor = true;
            this.btnTestUndo.Click += new System.EventHandler(this.btnTestUndo_Click);
            // 
            // btnOpenLogs
            // 
            this.btnOpenLogs.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnOpenLogs.Location = new System.Drawing.Point(170, 125);
            this.btnOpenLogs.Name = "btnOpenLogs";
            this.btnOpenLogs.Size = new System.Drawing.Size(154, 25);
            this.btnOpenLogs.TabIndex = 5;
            this.btnOpenLogs.Text = "Open Log Folder";
            this.btnOpenLogs.UseVisualStyleBackColor = true;
            this.btnOpenLogs.Click += new System.EventHandler(this.btnOpenLogs_Click);
            // 
            // btnReplayLast
            // 
            this.btnReplayLast.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnReplayLast.Location = new System.Drawing.Point(6, 150);
            this.btnReplayLast.Name = "btnReplayLast";
            this.btnReplayLast.Size = new System.Drawing.Size(154, 25);
            this.btnReplayLast.TabIndex = 6;
            this.btnReplayLast.Text = "Replay Last Session";
            this.btnReplayLast.UseVisualStyleBackColor = true;
            this.btnReplayLast.Click += new System.EventHandler(this.btnReplayLast_Click);
            // 
            // btnOpenReplay
            // 
            this.btnOpenReplay.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnOpenReplay.Location = new System.Drawing.Point(170, 150);
            this.btnOpenReplay.Name = "btnOpenReplay";
            this.btnOpenReplay.Size = new System.Drawing.Size(154, 25);
            this.btnOpenReplay.TabIndex = 7;
            this.btnOpenReplay.Text = "Open Replay Folder";
            this.btnOpenReplay.UseVisualStyleBackColor = true;
            this.btnOpenReplay.Click += new System.EventHandler(this.btnOpenReplay_Click);
            // 
            // btnReplayStart
            // 
            this.btnReplayStart.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnReplayStart.Location = new System.Drawing.Point(6, 180);
            this.btnReplayStart.Name = "btnReplayStart";
            this.btnReplayStart.Size = new System.Drawing.Size(100, 25);
            this.btnReplayStart.TabIndex = 8;
            this.btnReplayStart.Text = "Start Session";
            this.btnReplayStart.UseVisualStyleBackColor = true;
            this.btnReplayStart.Click += new System.EventHandler(this.btnReplayStart_Click);
            // 
            // btnReplayPause
            // 
            this.btnReplayPause.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnReplayPause.Location = new System.Drawing.Point(112, 180);
            this.btnReplayPause.Name = "btnReplayPause";
            this.btnReplayPause.Size = new System.Drawing.Size(100, 25);
            this.btnReplayPause.TabIndex = 9;
            this.btnReplayPause.Text = "Pause";
            this.btnReplayPause.UseVisualStyleBackColor = true;
            this.btnReplayPause.Click += new System.EventHandler(this.btnReplayPause_Click);
            // 
            // btnReplayResume
            // 
            this.btnReplayResume.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnReplayResume.Location = new System.Drawing.Point(218, 180);
            this.btnReplayResume.Name = "btnReplayResume";
            this.btnReplayResume.Size = new System.Drawing.Size(100, 25);
            this.btnReplayResume.TabIndex = 10;
            this.btnReplayResume.Text = "Resume";
            this.btnReplayResume.UseVisualStyleBackColor = true;
            this.btnReplayResume.Click += new System.EventHandler(this.btnReplayResume_Click);
            // 
            // btnReplayEnd
            // 
            this.btnReplayEnd.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnReplayEnd.Location = new System.Drawing.Point(6, 210);
            this.btnReplayEnd.Name = "btnReplayEnd";
            this.btnReplayEnd.Size = new System.Drawing.Size(312, 25);
            this.btnReplayEnd.TabIndex = 11;
            this.btnReplayEnd.Text = "End Session";
            this.btnReplayEnd.UseVisualStyleBackColor = true;
            this.btnReplayEnd.Click += new System.EventHandler(this.btnReplayEnd_Click);
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTestConnection.Location = new System.Drawing.Point(6, 125);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(154, 25);
            this.btnTestConnection.TabIndex = 4;
            this.btnTestConnection.Text = "Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblConnectionStatus.Location = new System.Drawing.Point(6, 82);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(78, 13);
            this.lblConnectionStatus.TabIndex = 3;
            this.lblConnectionStatus.Text = "Disconnected";
            // 
            // lblReplayStatus
            // 
            this.lblReplayStatus.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblReplayStatus.Location = new System.Drawing.Point(6, 95);
            this.lblReplayStatus.Name = "lblReplayStatus";
            this.lblReplayStatus.Size = new System.Drawing.Size(318, 30);
            this.lblReplayStatus.TabIndex = 4;
            this.lblReplayStatus.Text = "Replay idle. Click 'Replay Last Session' to recreate the last session.";
            // 
            // btnUpdateUrl
            // 
            this.btnUpdateUrl.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnUpdateUrl.Location = new System.Drawing.Point(270, 45);
            this.btnUpdateUrl.Name = "btnUpdateUrl";
            this.btnUpdateUrl.Size = new System.Drawing.Size(54, 23);
            this.btnUpdateUrl.TabIndex = 2;
            this.btnUpdateUrl.Text = "Update";
            this.btnUpdateUrl.UseVisualStyleBackColor = true;
            this.btnUpdateUrl.Click += new System.EventHandler(this.btnUpdateUrl_Click);
            // 
            // lblApiBase
            // 
            this.lblApiBase.AutoSize = true;
            this.lblApiBase.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblApiBase.Location = new System.Drawing.Point(6, 25);
            this.lblApiBase.Name = "lblApiBase";
            this.lblApiBase.Size = new System.Drawing.Size(111, 15);
            this.lblApiBase.TabIndex = 0;
            this.lblApiBase.Text = "Backend API URL:";
            // 
            // txtApiBase
            // 
            this.txtApiBase.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtApiBase.Location = new System.Drawing.Point(6, 45);
            this.txtApiBase.Name = "txtApiBase";
            this.txtApiBase.Size = new System.Drawing.Size(258, 23);
            this.txtApiBase.TabIndex = 1;
            // 
            // lblStatus
            // 
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblStatus.Location = new System.Drawing.Point(0, 1230);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblStatus.Size = new System.Drawing.Size(350, 25);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TaskPaneControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.grpTestUtils);
            this.Controls.Add(this.grpSettings);
            this.Controls.Add(this.grpLog);
            this.Controls.Add(this.grpSteps);
            this.Controls.Add(this.grpPlanner);
            this.Controls.Add(this.grpPlan);
            this.Controls.Add(this.btnPlan);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.btnPreview);
            this.Controls.Add(this.chkUseAI);
            this.Controls.Add(this.txtInstruction);
            this.Controls.Add(this.lblInstruction);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "TaskPaneControl";
            this.Size = new System.Drawing.Size(350, 1255);
            this.grpPlan.ResumeLayout(false);
            this.grpPlan.PerformLayout();
            this.grpPlanner.ResumeLayout(false);
            this.grpPlanner.PerformLayout();
            this.grpSteps.ResumeLayout(false);
            this.grpSteps.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.grpSettings.ResumeLayout(false);
            this.grpSettings.PerformLayout();
            this.grpTestUtils.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblInstruction;
        private System.Windows.Forms.TextBox txtInstruction;
        private System.Windows.Forms.CheckBox chkUseAI;
        private System.Windows.Forms.Button btnPlan;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.GroupBox grpPlan;
        private System.Windows.Forms.TextBox txtPlan;
        private System.Windows.Forms.GroupBox grpPlanner;
        private System.Windows.Forms.Label lblPlannerState;
        private System.Windows.Forms.TextBox txtPlannerQuestions;
        private System.Windows.Forms.TextBox txtPlannerAnswers;
        private System.Windows.Forms.Button btnPlannerSubmit;
        private System.Windows.Forms.GroupBox grpSteps;
        private System.Windows.Forms.CheckedListBox lstSteps;
        private System.Windows.Forms.Button btnRunSelectedStep;
        private System.Windows.Forms.Button btnRunCheckedSteps;
        private System.Windows.Forms.Button btnRunNextStep;
        private System.Windows.Forms.Button btnUndoLastStep;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.GroupBox grpSettings;
        private System.Windows.Forms.Label lblApiBase;
        private System.Windows.Forms.TextBox txtApiBase;
        private System.Windows.Forms.Button btnUpdateUrl;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Label lblReplayStatus;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Button btnOpenLogs;
        private System.Windows.Forms.Button btnReplayLast;
        private System.Windows.Forms.Button btnOpenReplay;
        private System.Windows.Forms.Button btnReplayStart;
        private System.Windows.Forms.Button btnReplayPause;
        private System.Windows.Forms.Button btnReplayResume;
        private System.Windows.Forms.Button btnReplayEnd;
        private System.Windows.Forms.GroupBox grpTestUtils;
        private System.Windows.Forms.Button btnTestUnits;
        private System.Windows.Forms.Button btnTestPlanes;
        private System.Windows.Forms.Button btnTestFaces;
        private System.Windows.Forms.Button btnTestUndo;
        private System.Windows.Forms.Label lblStatus;
    }
}
