using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PairingConfirm : QrsScrollPage
    {
        public PairingConfirm()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            lblPairingCode.Text = Context.QrCommandFlow.PairingRequest.PairingCode;

            var err = Context.QrCommandFlow.ErrorMessage;
            if (!string.IsNullOrWhiteSpace(err))
            {
                // ERROR - show ERROR then goto MainPage
                Device.BeginInvokeOnMainThread(() =>
                {
                    // stay on this page
                    DisplayAlert("ERROR", err, "OK");
                    App.GotoMainPage();
                });
            }
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            // continue Downloading Request

            //App.PrepareScanConfirmPage(); see same call below
            Context.QrCommandFlow.ClearRequest();
            Context.TunnelClient.DoNotResetMessageNumbers = true;

            IsLoading = true;
            
            await Task.Run(async () => await Context.DownloadRequestAsync(Context.QrCommandFlow.QrCommand));
            
            IsLoading = false;

            // "pairing then re-downloading request" execution flow has been completed
            // if there were Errors they are sitting in Context.QrCommandFlow
            // navigate to Confirm page, it will show the Error or the Data 

            App.PrepareScanConfirmPage();
            App.ScanConfirmPage.DownloadCompleted(); // this will load data into UI controls, or show the errors if any
            App.MainPage = App.ScanConfirmPage;

        }

        private async void Cancel_Clicked(object sender, EventArgs e)
        {
            App.GotoMainPage();
        }

        public void ShowButtons()
        {
            //btnYes.IsVisible = true;
            //btnNo.IsVisible = true;
        }
        public void HideButtons()
        {
            //btnYes.IsVisible = false;
            //btnNo.IsVisible = false;
        }


    }
}