
namespace Qrs.UI.WinForms
{
    partial class QrCodeWindow
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            contextMenuStrip1 = new ContextMenuStrip(components);
            copyQRCodeTextToolStripMenuItem = new ToolStripMenuItem();
            pnlMain = new Panel();
            pnlQrCode = new Panel();
            picQrCode = new PictureBox();
            pnlBottom = new Panel();
            lnkStatus = new LinkLabel();
            btnCancel = new Button();
            lblUseQrsApp = new Label();
            lblDescription = new LinkLabel();
            lblTitle = new Label();
            contextMenuStrip1.SuspendLayout();
            pnlMain.SuspendLayout();
            pnlQrCode.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picQrCode).BeginInit();
            pnlBottom.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { copyQRCodeTextToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(177, 26);
            // 
            // copyQRCodeTextToolStripMenuItem
            // 
            copyQRCodeTextToolStripMenuItem.Name = "copyQRCodeTextToolStripMenuItem";
            copyQRCodeTextToolStripMenuItem.Size = new Size(176, 22);
            copyQRCodeTextToolStripMenuItem.Text = "Copy QR Code Text";
            copyQRCodeTextToolStripMenuItem.Click += copyQRCodeTextToolStripMenuItem_Click;
            // 
            // pnlMain
            // 
            pnlMain.Controls.Add(pnlQrCode);
            pnlMain.Controls.Add(pnlBottom);
            pnlMain.Controls.Add(lblUseQrsApp);
            pnlMain.Controls.Add(lblDescription);
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Dock = DockStyle.Fill;
            pnlMain.Location = new Point(0, 0);
            pnlMain.Name = "pnlMain";
            pnlMain.Size = new Size(464, 516);
            pnlMain.TabIndex = 5;
            pnlMain.Paint += panel1_Paint;
            // 
            // pnlQrCode
            // 
            pnlQrCode.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pnlQrCode.Controls.Add(picQrCode);
            pnlQrCode.Dock = DockStyle.Fill;
            pnlQrCode.Location = new Point(0, 100);
            pnlQrCode.Name = "pnlQrCode";
            pnlQrCode.Padding = new Padding(5);
            pnlQrCode.Size = new Size(464, 326);
            pnlQrCode.TabIndex = 9;
            // 
            // picQrCode
            // 
            picQrCode.BackgroundImageLayout = ImageLayout.None;
            picQrCode.ContextMenuStrip = contextMenuStrip1;
            picQrCode.Dock = DockStyle.Fill;
            picQrCode.Location = new Point(5, 5);
            picQrCode.Margin = new Padding(5);
            picQrCode.Name = "picQrCode";
            picQrCode.Padding = new Padding(2);
            picQrCode.Size = new Size(454, 316);
            picQrCode.SizeMode = PictureBoxSizeMode.CenterImage;
            picQrCode.TabIndex = 8;
            picQrCode.TabStop = false;
            // 
            // pnlBottom
            // 
            pnlBottom.Controls.Add(lnkStatus);
            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Location = new Point(0, 426);
            pnlBottom.Name = "pnlBottom";
            pnlBottom.Size = new Size(464, 90);
            pnlBottom.TabIndex = 8;
            // 
            // lnkStatus
            // 
            lnkStatus.Dock = DockStyle.Bottom;
            lnkStatus.LinkColor = SystemColors.ControlText;
            lnkStatus.Location = new Point(0, 65);
            lnkStatus.Name = "lnkStatus";
            lnkStatus.Size = new Size(464, 25);
            lnkStatus.TabIndex = 4;
            lnkStatus.TabStop = true;
            lnkStatus.Text = "lnkStatus";
            lnkStatus.TextAlign = ContentAlignment.TopCenter;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.None;
            btnCancel.AutoEllipsis = true;
            btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCancel.Font = new Font("Segoe UI", 10F);
            btnCancel.Location = new Point(149, 20);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(167, 29);
            btnCancel.TabIndex = 3;
            btnCancel.Text = "CANCEL";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblUseQrsApp
            // 
            lblUseQrsApp.Dock = DockStyle.Top;
            lblUseQrsApp.Font = new Font("Segoe UI", 10F);
            lblUseQrsApp.Location = new Point(0, 70);
            lblUseQrsApp.Name = "lblUseQrsApp";
            lblUseQrsApp.Size = new Size(464, 30);
            lblUseQrsApp.TabIndex = 10;
            lblUseQrsApp.Text = "Please use QRS mobile app to scan this QR code";
            lblUseQrsApp.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDescription
            // 
            lblDescription.Dock = DockStyle.Top;
            lblDescription.Font = new Font("Segoe UI", 10F);
            lblDescription.Location = new Point(0, 40);
            lblDescription.Name = "lblDescription";
            lblDescription.Padding = new Padding(0, 5, 0, 5);
            lblDescription.Size = new Size(464, 30);
            lblDescription.TabIndex = 6;
            lblDescription.TabStop = true;
            lblDescription.Text = ".";
            lblDescription.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTitle
            // 
            lblTitle.Dock = DockStyle.Top;
            lblTitle.Font = new Font("Segoe UI", 14F);
            lblTitle.Location = new Point(0, 0);
            lblTitle.Name = "lblTitle";
            lblTitle.Padding = new Padding(0, 5, 0, 5);
            lblTitle.Size = new Size(464, 40);
            lblTitle.TabIndex = 5;
            lblTitle.Text = "Scan with QRS mobile app";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // QrCodeWindow
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(464, 516);
            Controls.Add(pnlMain);
            MinimizeBox = false;
            Name = "QrCodeWindow";
            SizeGripStyle = SizeGripStyle.Show;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Scan with QRS mobile app";
            contextMenuStrip1.ResumeLayout(false);
            pnlMain.ResumeLayout(false);
            pnlQrCode.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picQrCode).EndInit();
            pnlBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem copyQRCodeTextToolStripMenuItem;
        private Panel pnlMain;
        private Label lblTitle;
        private Panel pnlBottom;
        private LinkLabel lblDescription;
        private Panel pnlQrCode;
        private PictureBox picQrCode;
        private Button btnCancel;
        private LinkLabel lnkStatus;
        private Label lblUseQrsApp;
    }
}