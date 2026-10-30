using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using QRCoder;

namespace Qrs.App.FileManager.WinForms
{
    public partial class QrCodeWindow : Form
    {
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
            picQrCode.Image = GetQrImage(qrString);
            picQrCode.BackColor = Color.White;
            picQrCode.SizeMode = PictureBoxSizeMode.CenterImage; //PictureBoxSizeMode.Zoom;

            txtQrCode.Text = qrString + Environment.NewLine + Environment.NewLine + DateTime.Now.ToString();
            TopMost = true;
            Show();
        }


        private void btnCancel_Click(object sender, EventArgs e)
        {
            picQrCode.Image = null;
            txtQrCode.Text = "cleared before hiding this window";
            Hide();
        }
    }
}
