using Qrs.Mobile.App.Controls;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using BarcodeScanning;

namespace Qrs.Mobile.App
{
    public class QrsPageBase : ContentPage
    {
        private int scannerEnabled;

        public Qrs.Mobile.App.App App
        {
            get
            {
                return (Qrs.Mobile.App.App)Application.Current;
            }
        }

        public string AppVersion => Microsoft.Maui.ApplicationModel.AppInfo.Current.VersionString;
        public Qrs.Mobile.Client.Context.QrsAppContext Context
        {
            get
            {
                return App.QrsAppContext;
            }
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            Context.LogDebug(message);
        }

        public bool IsDebug
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        public void SetError(TextInputBase textEdit, string errorText)
        {
            textEdit.ErrorText = errorText;
            //textEdit.TextColor = textEdit.ErrorColor;
            textEdit.HasError = true;
        }

        public void SetValid(TextInputBase textEdit)
        {
            textEdit.ErrorText = "";
            //textEdit.TextColor = Color.Default;
            textEdit.HasError = false;
        }

        public void Trim(TextInputBase textEdit)
        {
            textEdit.Text = textEdit.Text?.Trim() ?? string.Empty;
        }

        public bool IsValidPassword(TextInputBase password1, TextInputBase password2)
        {
            Trim(password1);
            Trim(password2);
            if (password1.Text != password2.Text)
            {
                SetError(password2, "passwords do not match");
                return false;
            }
            // if both inputs are the same, then just validate ONE of them to reuse the validation logic
            return IsValidPassword(password1);
        }

        public bool IsValidPassword(TextInputBase password1)
        {
            Trim(password1);
            if (password1.Text.Length < 8)
            {
                SetError(password1, "must be at least 8 characters");
                return false;
            }
            return true;
        }

        public bool IsValidKeyName(TextInputBase keyName)
        {
            Trim(keyName);
            if (keyName.Text.Length < 1)
            {
                SetError(keyName, "Key Name is required");
                return false;
            }
            if (keyName.Text.Length > 100)
            {
                SetError(keyName, $"must be < 100 characters ({keyName.Text.Length})");
                return false;
            }
            return true;
        }

        public void ShowError(string message)
        {
            LogDebug($"ERROR - ShowError(message='{message}')");
            Device.BeginInvokeOnMainThread(() =>
            {
                // stay on this page
                DisplayAlert("ERROR", message, "OK");
            });
        }

        public async Task GoToNewRootAsync(Page newRootPage)
        {
            Navigation.InsertPageBefore(newRootPage, Navigation.NavigationStack.First());
            await Navigation.PopToRootAsync();
        }

        public bool IsAndroid => DeviceInfo.Current.Platform == DevicePlatform.Android;
        public bool IsIOS => DeviceInfo.Current.Platform == DevicePlatform.iOS;

        public void Vibrate()
        {
            // should be called from UI thread (for now it is b/c it is called from the event handler of the CameraView_OnDetectionFinished() event)
            if (IsAndroid || IsIOS)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }

        /// <summary>
        /// Tears down MAUI's VisualDiagnosticsOverlay while the current platform views are still alive.
        /// The overlay caches a ViewGroup of the current root view and removes its graphics view from it
        /// when the Android root view changes (i.e. when Application.MainPage is replaced). On a page
        /// hosting BarcodeScanning's CameraView that cached ViewGroup is already disposed by then, which
        /// crashes with ObjectDisposedException. WindowHandler.OnRootViewChanged skips the teardown when
        /// the overlay is no longer initialized, so doing it here up front avoids the crash.
        /// </summary>
        protected void DeinitializeVisualDiagnosticsOverlay()
        {
            var overlay = (Window as IWindow)?.VisualDiagnosticsOverlay;
            if (overlay is not null && overlay.IsPlatformViewInitialized)
            {
                overlay.Deinitialize();
            }
        }

        public bool DisableScanner(CameraView objScanner, Microsoft.Maui.Controls.StackLayout stlBlackScreen = null)
        {
            var wasEnabled = Interlocked.Exchange(ref scannerEnabled, 0) != 0;

            // block the Camera Surface immediately by showing the black layout block over it
            // this has been the most efficient way to immediately show user that scanning has completed (i.e. stop the video stream)
            // until we donwnload request, we dont know what page is next (i.e. Pairing or Scan Confirm Page)
            // so we cannot just navigate to Scan Confirm Page and await downloading there (as we do in QRID app)
            // since we cannot move to the next page just yet, at least we need to show the user that scanning has completed
            // and show a full-screen black block with the spinning wheel (<ActivityIndicator/> element in XAML)
            // NOTE: this must run synchronously when we are already on the UI thread, otherwise the camera teardown
            // would be queued after a root page swap and the native BarcodeView could already be disposed (Android crash)
            void Teardown()
            {
                if (stlBlackScreen != null) stlBlackScreen.IsVisible = true;
                if (objScanner is null) return;
                objScanner.CameraEnabled = false;
                //objScanner.IsEnabled = false;
                //objScanner.IsVisible = false;
            }

            if (MainThread.IsMainThread)
            {
                Teardown();
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(Teardown);
            }
            LogDebug($"DisableScanner() - hiding the scanner");
            return wasEnabled;
        }

        public void EnableScanner(CameraView objScanner, Microsoft.Maui.Controls.StackLayout stlBlackScreen = null)
        {
            Interlocked.Exchange(ref scannerEnabled, 1);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (stlBlackScreen != null) stlBlackScreen.IsVisible = false;
                if (objScanner is null) return;
                //objScanner.IsVisible = true;
                objScanner.CameraEnabled = true;
                objScanner.IsEnabled = true;
            });
            LogDebug($"EnableScanner() - showing the scanner");
        }

        public async Task Logout(bool askConfirmation = true)
        {
            if (askConfirmation)
            {
                bool logout = await DisplayAlert("Logout?", "Logout only if you have already created a Backup Image of your Master Key. Do you still want to Logout and unload your Master Key?", "Yes", "No");
                if (!logout) return;
            }

            // wipe the keys and all the data held by the flows, and generate a new pairing key
            // (off the UI thread - secure storage access and post-quantum key generation are slow)
            await Task.Run(() => Context.Logout());

            // re-init the UI - discard all page instances (they may still have a parent from the
            // current NavigationPage) and start over with a new NavigationPage rooted at a new Welcome page
            App.GotoWelcomePage();
        }

    }
}
