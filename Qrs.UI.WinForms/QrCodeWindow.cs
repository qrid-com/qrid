using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QRCoder;

namespace Qrs.UI.WinForms
{
    public partial class QrCodeWindow : Form
    {
        private string QrCodeString = string.Empty;
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Action? OnCancelRequested { get; set; }

        public QrCodeWindow()
        {
            InitializeComponent();
        }

        public Image GetQrImage(string qrString)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            var qrCodeImage = qrCode.GetGraphic(4);

            //using (var stream = new MemoryStream())
            //{
            //    qrCodeImage.Save(stream, ImageFormat.Png);
            //    stream.Seek(0, SeekOrigin.Begin);
            //    ImagePath = new Bitmap(stream);
            //}

            return qrCodeImage;
        }

        public void ShowQrCodeWindow(string qrString)
        {
            QrCodeString = qrString;
            picQrCode.Image = GetQrImage(qrString);
            picQrCode.BackColor = Color.White;
            picQrCode.SizeMode = PictureBoxSizeMode.CenterImage; //PictureBoxSizeMode.Zoom;

            TopMost = true;
            Show();
        }

        public void SetDescription(string title, string description)
        {
            lblTitle.Text = title;
            lblDescription.Text = description;
        }

        public void SetTunnelServiceUri(string uri)
        {
            lnkStatus.Text = uri;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            QrCodeString = string.Empty;
            picQrCode.Image = null;
            Hide();
            OnCancelRequested?.Invoke();
        }

        private void copyQRCodeTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(QrCodeString);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
