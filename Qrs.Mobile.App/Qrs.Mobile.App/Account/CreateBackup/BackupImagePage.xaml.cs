using System;
using System.IO;
using Microsoft.Maui.Controls.Xaml;
using QRCoder;

namespace Qrs.Mobile.App.Account.CreateBackup
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupImagePage : QrsScrollPage
    {
        public BackupImagePage()
        {
            InitializeComponent();
        }


        public void SetQrCode(string qrCode)
        {
            using var qrCodeData = QRCodeGenerator.GenerateQrCode(qrCode, QRCodeGenerator.ECCLevel.Q);
            using var pngQrCode = new PngByteQRCode(qrCodeData);
            var imageBytes = pngQrCode.GetGraphic(20);
            BackupQrCodeImage.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
        }

        public void SetKeyName(string keyName)
        {
            KeyName.Text = keyName;
        }

        private bool IsValid()
        {
            // todo - VerificationCode - not empty, numbers
            return true;
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            // success, go to next page
            await Navigation.PushAsync(App.CbCongratulationsPage);
            
        }


    }
}