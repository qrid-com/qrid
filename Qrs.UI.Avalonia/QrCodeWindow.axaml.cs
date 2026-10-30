using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using QRCoder;
using System.IO;

namespace Qrs.UI.Avalonia
{
    public partial class QrCodeWindow : Window
    {
        private string QrCodeString = string.Empty;

        public QrCodeWindow()
        {
            InitializeComponent();
        }

        public Bitmap GetQrImage(string qrString)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrString, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] pngBytes = qrCode.GetGraphic(4);
            using var pngStream = new MemoryStream(pngBytes);
            return new Bitmap(pngStream);
        }

        public void ShowQrCodeWindow(string qrString)
        {
            QrCodeString = qrString;
            picQrCode.Source = GetQrImage(qrString);
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

        private void btnCancel_Click(object? sender, RoutedEventArgs e)
        {
            QrCodeString = string.Empty;
            picQrCode.Source = null;
            Hide();
        }

        private void copyQRCodeText_Click(object? sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(QrCodeString))
            {
                Dispatcher.UIThread.Post(async () =>
                {
                    var topLevel = TopLevel.GetTopLevel(this);
                    if (topLevel?.Clipboard != null)
                        await topLevel.Clipboard.SetTextAsync(QrCodeString);
                });
            }
        }
    }
}
