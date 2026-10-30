using System;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using BarcodeScanning;

namespace Qrs.Mobile.App.Account.ExistingKey
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BackupScanPage : QrsPageBase
    {
        public BackupScanPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (await Methods.AskForRequiredPermissionAsync())
            {
                EnableScanner(objScanner, stlBlackScreen);
            }
            this.lblTitle.Text = "Scanning Backup Image...";
        }

        protected override void OnDisappearing()
        {
            DisableScanner(objScanner, stlBlackScreen);
            base.OnDisappearing();
        }

        public bool NoPreviousPage()
        {
            return (Navigation.NavigationStack.Count == 1 && Navigation.NavigationStack[0] == this);

        }
        private async void StopScanning_Clicked(object sender, EventArgs e)
        {
            DisableScanner(objScanner, stlBlackScreen); // do we need this here?
            App.PrepareMainPages();
            await Navigation.PopAsync();
            //App.MainPage = App.NavPage;

        }

        private async void CameraView_OnDetectionFinished(object sender, OnDetectionFinishedEventArg e)
        {
            var result = e.BarcodeResults.FirstOrDefault();
            if (result is null || !DisableScanner(objScanner, stlBlackScreen))
            {
                return;
            }

            // let the user know that QR code has been identified
            Vibrate();
            LogDebug($"CameraView_OnDetectionFinished - Vibrate() called");

            LogDebug($"SCANNED QR CODE TEXT LENGTH: {result.RawValue?.Length}");

            var qbi = Context.ParseBackupImage(result.RawValue);
            if (qbi.IsValidBackupImage)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    // save QR code and go to Password page 
                    Context.ExistingUserFlow.QrBackupImage = qbi;
                    Context.ExistingUserFlow.QrCodeString = result.RawValue;
                    App.ExBackupPasswordPage.SetKeyName(qbi.Name);
                    await Navigation.PushAsync(App.ExBackupPasswordPage);
                });
            }
            else
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    
                    
                    await DisplayAlert("Invalid Backup Image", "The scanned QR code is not a valid backup image. Please try again.", "OK");
                    LogDebug($"Invalid Backup Image");
                    App.PrepareMainPages();
                    await Navigation.PopAsync();
                });
            }
        }



        

    }
}