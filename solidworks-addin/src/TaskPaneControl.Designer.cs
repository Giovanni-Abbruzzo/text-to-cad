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
            this.lblInstruction = new System.Windows.Forms.Label();
            this.txtInstruction = new System.Windows.Forms.TextBox();
            this.chkUseAI = new System.Windows.Forms.CheckBox();
            this.btnPreview = new System.Windows.Forms.Button();
            this.btnExecute = new System.Windows.Forms.Button();
            this.grpPlan = new System.Windows.Forms.GroupBox();
            this.txtPlan = new System.Windows.Forms.TextBox();
            this.grpLog = new System.Windows.Forms.GroupBox();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.grpSettings = new System.Windows.Forms.GroupBox();
            this.btnOpenLogs = new System.Windows.Forms.Button();
            this.btnTestConnection = new System.Windows.Forms.Button();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.btnUpdateUrl = new System.Windows.Forms.Button();
            this.lblApiBase = new System.Windows.Forms.Label();
            this.txtApiBase = new System.Windows.Forms.TextBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.grpPlan.SuspendLayout();
            this.grpLog.SuspendLayout();
            this.grpSettings.SuspendLayout();
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
            // lblInstruction
            // 
            this.lblInstruction.AutoSize = true;
            this.lblInstruction.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblInstruction.Location = new System.Drawing.Point(10, 50);
            this.lblInstruction.Name = "lblInstruction";
            this.lblInstruction.Size = new System.Drawing.Size(120, 15);
            this.lblInstruction.TabIndex = 1;
            this.lblInstruction.Text = "CAD Instruction:";
            // 
            // txtInstruction
            // 
            this.txtInstruction.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtInstruction.Location = new System.Drawing.Point(10, 70);
            this.txtInstruction.Multiline = true;
            this.txtInstruction.Name = "txtInstruction";
            this.txtInstruction.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtInstruction.Size = new System.Drawing.Size(330, 60);
            this.txtInstruction.TabIndex = 2;
            this.txtInstruction.Enter += new System.EventHandler(this.txtInstruction_Enter);
            this.txtInstruction.Leave += new System.EventHandler(this.txtInstruction_Leave);
            // 
            // chkUseAI
            // 
            this.chkUseAI.AutoSize = true;
            this.chkUseAI.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkUseAI.Location = new System.Drawing.Point(10, 140);
            this.chkUseAI.Name = "chkUseAI";
            this.chkUseAI.Size = new System.Drawing.Size(200, 19);
            this.chkUseAI.TabIndex = 3;
            this.chkUseAI.Text = "Use AI Parsing (requires API key)";
            this.chkUseAI.UseVisualStyleBackColor = true;
            // 
            // btnPreview
            // 
            this.btnPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(215)))));
            this.btnPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPreview.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnPreview.ForeColor = System.Drawing.Color.White;
            this.btnPreview.Location = new System.Drawing.Point(10, 170);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(160, 35);
            this.btnPreview.TabIndex = 4;
            this.btnPreview.Text = "🔍 Preview (Dry Run)";
            this.btnPreview.UseVisualStyleBackColor = false;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // btnExecute
            // 
            this.btnExecute.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(0)))));
            this.btnExecute.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExecute.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnExecute.ForeColor = System.Drawing.Color.White;
            this.btnExecute.Location = new System.Drawing.Point(180, 170);
            this.btnExecute.Name = "btnExecute";
            this.btnExecute.Size = new System.Drawing.Size(160, 35);
            this.btnExecute.TabIndex = 5;
            this.btnExecute.Text = "⚙️ Execute";
            this.btnExecute.UseVisualStyleBackColor = false;
            this.btnExecute.Click += new System.EventHandler(this.btnExecute_Click);
            // 
            // grpPlan
            // 
            this.grpPlan.Controls.Add(this.txtPlan);
            this.grpPlan.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpPlan.Location = new System.Drawing.Point(10, 215);
            this.grpPlan.Name = "grpPlan";
            this.grpPlan.Size = new System.Drawing.Size(330, 120);
            this.grpPlan.TabIndex = 6;
            this.grpPlan.TabStop = false;
            this.grpPlan.Text = "📋 Execution Plan";
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
            // grpLog
            // 
            this.grpLog.Controls.Add(this.txtLog);
            this.grpLog.Controls.Add(this.btnClearLog);
            this.grpLog.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpLog.Location = new System.Drawing.Point(10, 345);
            this.grpLog.Name = "grpLog";
            this.grpLog.Size = new System.Drawing.Size(330, 200);
            this.grpLog.TabIndex = 7;
            this.grpLog.TabStop = false;
            this.grpLog.Text = "📝 Log";
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
            this.grpSettings.Controls.Add(this.btnOpenLogs);
            this.grpSettings.Controls.Add(this.btnTestConnection);
            this.grpSettings.Controls.Add(this.lblConnectionStatus);
            this.grpSettings.Controls.Add(this.btnUpdateUrl);
            this.grpSettings.Controls.Add(this.lblApiBase);
            this.grpSettings.Controls.Add(this.txtApiBase);
            this.grpSettings.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpSettings.Location = new System.Drawing.Point(10, 555);
            this.grpSettings.Name = "grpSettings";
            this.grpSettings.Size = new System.Drawing.Size(330, 140);
            this.grpSettings.TabIndex = 8;
            this.grpSettings.TabStop = false;
            this.grpSettings.Text = "⚙️ Settings";
            // 
            // btnOpenLogs
            // 
            this.btnOpenLogs.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnOpenLogs.Location = new System.Drawing.Point(170, 105);
            this.btnOpenLogs.Name = "btnOpenLogs";
            this.btnOpenLogs.Size = new System.Drawing.Size(154, 25);
            this.btnOpenLogs.TabIndex = 5;
            this.btnOpenLogs.Text = "📂 Open Log Folder";
            this.btnOpenLogs.UseVisualStyleBackColor = true;
            this.btnOpenLogs.Click += new System.EventHandler(this.btnOpenLogs_Click);
            // 
            // btnTestConnection
            // 
            this.btnTestConnection.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.btnTestConnection.Location = new System.Drawing.Point(6, 105);
            this.btnTestConnection.Name = "btnTestConnection";
            this.btnTestConnection.Size = new System.Drawing.Size(154, 25);
            this.btnTestConnection.TabIndex = 4;
            this.btnTestConnection.Text = "🔌 Test Connection";
            this.btnTestConnection.UseVisualStyleBackColor = true;
            this.btnTestConnection.Click += new System.EventHandler(this.btnTestConnection_Click);
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.Font = new System.Drawing.Font("Segoe UI", 8F);
            this.lblConnectionStatus.Location = new System.Drawing.Point(6, 82);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(90, 13);
            this.lblConnectionStatus.TabIndex = 3;
            this.lblConnectionStatus.Text = "● Disconnected";
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
            this.lblApiBase.Size = new System.Drawing.Size(95, 15);
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
            this.lblStatus.Location = new System.Drawing.Point(0, 705);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(10, 5, 10, 5);
            this.lblStatus.Size = new System.Drawing.Size(350, 25);
            this.lblStatus.TabIndex = 9;
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
            this.Controls.Add(this.grpSettings);
            this.Controls.Add(this.grpLog);
            this.Controls.Add(this.grpPlan);
            this.Controls.Add(this.btnExecute);
            this.Controls.Add(this.btnPreview);
            this.Controls.Add(this.chkUseAI);
            this.Controls.Add(this.txtInstruction);
            this.Controls.Add(this.lblInstruction);
            this.Controls.Add(this.lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "TaskPaneControl";
            this.Size = new System.Drawing.Size(350, 730);
            this.grpPlan.ResumeLayout(false);
            this.grpPlan.PerformLayout();
            this.grpLog.ResumeLayout(false);
            this.grpSettings.ResumeLayout(false);
            this.grpSettings.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblInstruction;
        private System.Windows.Forms.TextBox txtInstruction;
        private System.Windows.Forms.CheckBox chkUseAI;
        private System.Windows.Forms.Button btnPreview;
        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.GroupBox grpPlan;
        private System.Windows.Forms.TextBox txtPlan;
        private System.Windows.Forms.GroupBox grpLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button btnClearLog;
        private System.Windows.Forms.GroupBox grpSettings;
        private System.Windows.Forms.Label lblApiBase;
        private System.Windows.Forms.TextBox txtApiBase;
        private System.Windows.Forms.Button btnUpdateUrl;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Button btnTestConnection;
        private System.Windows.Forms.Button btnOpenLogs;
        private System.Windows.Forms.Label lblStatus;
    }
}
