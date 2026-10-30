using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace Qrs.Mobile.App.Account.ExistingKey
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupPasswordPage : QrsScrollPage
    {
        public BackupPasswordPage()
        {
            InitializeComponent();
        }

        public void SetKeyName(string keyName)
        {
            KeyName.Text = keyName;
            if (!string.IsNullOrWhiteSpace(keyName))
            {
                // Key Name came from the QR code - do not let the user change it
                KeyName.IsReadOnly = true;
            }
            else
            {
                // No Key Name in the QR code - the user must type it in.
                // Explicitly make it editable because this long-living page/control
                // instance may have been left read-only by a previous flow.
                KeyName.IsReadOnly = false;
            }
        }
        public string GetKeyName()
        {
            return string.IsNullOrWhiteSpace(Context.ExistingUserFlow.QrBackupImage.Name)
                ? KeyName.Text
                : Context.ExistingUserFlow.QrBackupImage.Name;
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            // validate password
            if (!IsValidPassword(Password1))
                return;

            Trim(KeyName);

            IsLoading = true;

            //use password to decrypt Master Key from Backup Image
            var result = await Task.Run(async () => await Context.ExistingMasterKeyAsync(Password1.Text, 
                Context.ExistingUserFlow.QrBackupImage.Key,
                GetKeyName(),
                Context.ExistingUserFlow.QrBackupImage.ContainsName));

            IsLoading = false;

            if (!result)
            {
                // error, stay on this page
                await DisplayAlert("ERROR", "Provided Password might be wrong - unexpected error while trying to load your Master Key. Please try again", "OK");
                return;
            }

            MainThread.BeginInvokeOnMainThread(() => Password1.Text = string.Empty);

            // goto Congrats Page
            App.ExCongratulationsPage.MessageText = true ? "Success!" : "Error";
            await Navigation.PushAsync(App.ExCongratulationsPage); //App.MainPage = App.ExCongratulationsPage;
        }
    }
}