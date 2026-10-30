
namespace Qrs.App.FileManager.WinForms
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
            picQrCode = new System.Windows.Forms.PictureBox();
            txtQrCode = new System.Windows.Forms.TextBox();
            btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)picQrCode).BeginInit();
            SuspendLayout();
            // 
            // picQrCode
            // 
            picQrCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            picQrCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            picQrCode.Location = new System.Drawing.Point(13, 11);
            picQrCode.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            picQrCode.Name = "picQrCode";
            picQrCode.Size = new System.Drawing.Size(318, 323);
            picQrCode.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            picQrCode.TabIndex = 0;
            picQrCode.TabStop = false;
            // 
            // txtQrCode
            // 
            txtQrCode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            txtQrCode.Location = new System.Drawing.Point(348, 11);
            txtQrCode.Multiline = true;
            txtQrCode.Name = "txtQrCode";
            txtQrCode.Size = new System.Drawing.Size(433, 247);
            txtQrCode.TabIndex = 1;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.Location = new System.Drawing.Point(533, 293);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(75, 23);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "CANCEL";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // QrCodeWindow
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(801, 349);
            Controls.Add(btnCancel);
            Controls.Add(txtQrCode);
            Controls.Add(picQrCode);
            Name = "QrCodeWindow";
            SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Scan with QRS mobile app";
            ((System.ComponentModel.ISupportInitialize)picQrCode).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox picQrCode;
        private System.Windows.Forms.TextBox txtQrCode;
        private System.Windows.Forms.Button btnCancel;
    }
}