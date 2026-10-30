using Qrid.Common.Data;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using BarcodeScanning;
using Microsoft.Maui.Controls.PlatformConfiguration;
using CommunityToolkit.Maui.Behaviors;
using Microsoft.Maui.ApplicationModel;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanPage : QrsPageBase
    {
        public ScanPage()
        {
            InitializeComponent();
        }

        protected void SetBlackStatusBar()
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // <mct:StatusBarBehavior  x:Name="statusBar" StatusBarColor="Black" StatusBarStyle="LightContent" />
                    // it is important (for some reason) that this method sets the same values also already set in XAML for BOTH properties
                    // otherwise it is crashing in IOS even though we use BeginInvokeOnMainThread
                    statusBar.StatusBarColor = Colors.Black;
                    statusBar.StatusBarStyle = CommunityToolkit.Maui.Core.StatusBarStyle.LightContent;
                });
            }
        }

        protected void SetDefaultStatusBar()
        {
            if (OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
            {
                statusBar.StatusBarColor = Color.FromArgb("3f77f6");
                statusBar.StatusBarStyle = CommunityToolkit.Maui.Core.StatusBarStyle.Default;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            SetBlackStatusBar();

            if (!string.IsNullOrWhiteSpace(Context.QrCommandFlow.ManualQrCodeString) )
            {
                // user has manually copy-pasted QR CODE via ScanInputPage
                // clear it before processing
                var qrCodeString = Context.QrCommandFlow.ManualQrCodeString;
                Context.QrCommandFlow.ManualQrCodeString = null;

                // start processing QrCode and do NOT wait for its results
                Task.Run(() => OnScanResult(qrCodeString));
                return;
            }
            if (await Methods.AskForRequiredPermissionAsync())
            {
                EnableScanner(objScanner, stlBlackScreen);
            }
            var uname = ""; // todo - show Master Key Name
            this.lblTitle.Text = uname.Length > 1 ? uname : "Scanning...";
        }

        protected override void OnDisappearing()
        {
            DisableScanner(objScanner, stlBlackScreen);
            base.OnDisappearing();

            SetDefaultStatusBar();
        }

        public bool NoPreviousPage()
        {
            return (Navigation.NavigationStack.Count == 1 && Navigation.NavigationStack[0] == this);
                
        }
        private async void StopScanning_Clicked(object sender, EventArgs e)
        {
            DisableScanner(objScanner, stlBlackScreen);

            // let the teardown complete (above call) before swapping the root page (below)
            await Task.Yield();
            MainThread.BeginInvokeOnMainThread(() => 
            {
                // must happen before the native camera view is disposed and before the root page is swapped
                DeinitializeVisualDiagnosticsOverlay();
                // this will swap the root page aka Application.MainPage = Zzz
                App.GotoMainPage();
            });
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

            await OnScanResult(result.RawValue);
            LogDebug($"CameraView_OnDetectionFinished - finished");
        }

        private async Task OnScanResult(string result)
        {
            LogDebug($"SCANNED QR CODE TEXT LENGTH: {result?.Length}");
            var qcmd = QrCommand.Create(result);
            if (qcmd.IsValidCmd)
            {
                await OnScanResult(qcmd);
            }
            else
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Show error, then goto MainPage
                    DisplayAlert("ERROR", $"PLEASE TRY AGAIN - non-supported QR CODE - {qcmd.Error}", "OK");
                    LogDebug($"non-supported QR CODE, error: '{qcmd.Error}'");
                    DeinitializeVisualDiagnosticsOverlay();
                    App.GotoMainPage();
                });
            }
        }

        private async Task OnScanResult(QrCommand qrCommand)
        {
            // stop scanning
            DisableScanner(objScanner, stlBlackScreen);
            Task.Run(async () => { await DownloadRequest(qrCommand); });
            LogDebug($"OnScanResult - DownloadRequest called without awaiting");
        }

        private async Task DownloadRequest(QrCommand qrCommand)
        {
            LogDebug($"DownloadRequest - before awaiting of Context.DownloadRequestAsync");
            await Context.DownloadRequestAsync(qrCommand);
            LogDebug($"DownloadRequest - after awaiting");

            if (Context.QrCommandFlow.HasPairing())
            {
                // PAIRING
                Device.BeginInvokeOnMainThread(() =>
                {
                    // goto Pairing page
                    DeinitializeVisualDiagnosticsOverlay();
                    App.PreparePairingPage();
                    App.MainPage = App.PairingPage;
                });
            }
            else
            {
                // PAYLOAD or ERROR
                Device.BeginInvokeOnMainThread(() =>
                {
                    // go to Scan Confirm page, with error or success
                    DeinitializeVisualDiagnosticsOverlay();
                    App.PrepareScanConfirmPage();
                    App.ScanConfirmPage.DownloadCompleted();
                    App.MainPage = App.ScanConfirmPage;
                });
            }

            
        }


    }
}