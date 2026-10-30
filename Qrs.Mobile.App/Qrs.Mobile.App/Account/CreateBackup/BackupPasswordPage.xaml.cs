using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace Qrs.Mobile.App.Account.CreateBackup
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupPasswordPage : QrsScrollPage
    {
        public BackupPasswordPage()
        {
            InitializeComponent();
        }

        public void Email_Completed(object sender, EventArgs e)
        {
            string text = ((Entry)sender).Text;
        }
        
        internal void SetKeyName(string keyName)
        {
            KeyName.Text = keyName;
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            // validate - compare pwd for typos, min lenght
            if (!IsValidPassword(Password1, Password2))
                return;

            IsLoading = true;

            var result = await Task.Run(async () => await Context.BackupMasterKeyAsync(Password1.Text));

            IsLoading = false;

            if (!result)
            {
                // error, stay on this page
                await DisplayAlert("ERROR", "Unexpected error while trying to backup your Master Key", "OK");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Password1.Text = string.Empty;
                Password2.Text = string.Empty;
            });

            App.CbBackupImagePage.SetKeyName(Context.OrangeKeys.KeyName);
            // todo - could use special base45 encoding to make QR code smaller
            App.CbBackupImagePage.SetQrCode(Context.BackupFlow.QrCodeString);

            await Navigation.PushAsync(App.CbBackupImagePage);
        }
    }
}