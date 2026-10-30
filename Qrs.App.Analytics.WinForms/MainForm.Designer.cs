namespace Qrs.App.Analytics.WinForms
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            tabControlMain = new TabControl();
            tabLog = new TabPage();
            rtbLog = new RichTextBox();
            panel1 = new Panel();
            btnUpdateLogs = new Button();
            chkTimerEnabled = new CheckBox();
            txtSettingsFile = new TextBox();
            timer1 = new System.Windows.Forms.Timer(components);
            tabControlMain.SuspendLayout();
            tabLog.SuspendLayout();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlMain
            // 
            tabControlMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControlMain.Controls.Add(tabLog);
            tabControlMain.Location = new Point(0, 55);
            tabControlMain.Margin = new Padding(3, 2, 3, 2);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(797, 284);
            tabControlMain.TabIndex = 0;
            // 
            // tabLog
            // 
            tabLog.Controls.Add(rtbLog);
            tabLog.Location = new Point(4, 24);
            tabLog.Margin = new Padding(3, 2, 3, 2);
            tabLog.Name = "tabLog";
            tabLog.Padding = new Padding(3, 2, 3, 2);
            tabLog.Size = new Size(789, 256);
            tabLog.TabIndex = 0;
            tabLog.Text = "Log";
            tabLog.UseVisualStyleBackColor = true;
            // 
            // rtbLog
            // 
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Location = new Point(3, 2);
            rtbLog.Margin = new Padding(3, 2, 3, 2);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(783, 252);
            rtbLog.TabIndex = 0;
            rtbLog.Text = "";
            // 
            // panel1
            // 
            panel1.Controls.Add(btnUpdateLogs);
            panel1.Controls.Add(chkTimerEnabled);
            panel1.Controls.Add(txtSettingsFile);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Margin = new Padding(3, 2, 3, 2);
            panel1.Name = "panel1";
            panel1.Size = new Size(797, 50);
            panel1.TabIndex = 1;
            // 
            // btnUpdateLogs
            // 
            btnUpdateLogs.Location = new Point(528, 12);
            btnUpdateLogs.Name = "btnUpdateLogs";
            btnUpdateLogs.Size = new Size(90, 23);
            btnUpdateLogs.TabIndex = 2;
            btnUpdateLogs.Text = "Update Logs";
            btnUpdateLogs.UseVisualStyleBackColor = true;
            btnUpdateLogs.Click += btnUpdateLogs_Click;
            // 
            // chkTimerEnabled
            // 
            chkTimerEnabled.AutoSize = true;
            chkTimerEnabled.Checked = true;
            chkTimerEnabled.CheckState = CheckState.Checked;
            chkTimerEnabled.Location = new Point(405, 15);
            chkTimerEnabled.Name = "chkTimerEnabled";
            chkTimerEnabled.Size = new Size(101, 19);
            chkTimerEnabled.TabIndex = 1;
            chkTimerEnabled.Text = "Timer Enabled";
            chkTimerEnabled.UseVisualStyleBackColor = true;
            chkTimerEnabled.CheckedChanged += chkTimerEnabled_CheckedChanged;
            // 
            // txtSettingsFile
            // 
            txtSettingsFile.Location = new Point(7, 12);
            txtSettingsFile.Name = "txtSettingsFile";
            txtSettingsFile.ReadOnly = true;
            txtSettingsFile.Size = new Size(384, 23);
            txtSettingsFile.TabIndex = 0;
            // 
            // timer1
            // 
            timer1.Interval = 6000;
            timer1.Tick += Timer_Tick;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(797, 338);
            Controls.Add(panel1);
            Controls.Add(tabControlMain);
            Name = "MainForm";
            Text = "Form1";
            Load += MainForm_Load;
            tabControlMain.ResumeLayout(false);
            tabLog.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControlMain;
        private TabPage tabLog;
        private Panel panel1;
        private RichTextBox rtbLog;
        private System.Windows.Forms.Timer timer1;
        private CheckBox chkTimerEnabled;
        private TextBox txtSettingsFile;
        private Button btnUpdateLogs;
    }
}
