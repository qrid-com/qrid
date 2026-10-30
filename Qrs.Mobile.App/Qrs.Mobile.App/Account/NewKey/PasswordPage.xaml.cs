using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace Qrs.Mobile.App.Account.NewKey
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PasswordPage : QrsScrollPage
    {
        public PasswordPage()
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
            var isValidKeyName = IsValidKeyName(KeyName);
            var isValidPassword = IsValidPassword(Password1, Password2);
            if (!isValidKeyName || !isValidPassword)
                return;

            IsLoading = true;

            var result = await Task.Run(async () => await Context.NewMasterKeyAsync(Password1.Text, KeyName.Text));
            
            IsLoading = false;

            if (!result)
            {
                // error, stay on this page
                await DisplayAlert("ERROR", "Unexpected error while trying to create Master Key", "OK");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Password1.Text = string.Empty;
                Password2.Text = string.Empty;
            });

            
            App.BackupImagePage.SetKeyName(Context.NewUserFlow.KeyName);
            // todo - could use special base45 encoding to make QR code smaller
            App.BackupImagePage.SetQrCode(Context.GetQrCodeString(Context.NewUserFlow.PasswordEncryptedMasterKey, Context.NewUserFlow.KeyName));

            await Navigation.PushAsync(App.BackupImagePage);
        }

    }
}