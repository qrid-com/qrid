using System;
using System.Diagnostics;
using Microsoft.Maui.Controls.Xaml;
using System.Threading.Tasks;
using Qrs.Mobile.Client.Data;

using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;
using Utils = Qrid.Common.Crypto.CryptoUtils;
using Qrs.Mobile.Client;
using Qrs.Mobile.App.MobileDevice;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;

namespace Qrs.Mobile.App
{
    public partial class App : Application
    {
        public NavigationPage NavPage;

        public Qrs.Mobile.App.Account.WelcomePage WelcomePage;
        // New Key flow
        public Qrs.Mobile.App.Account.NewKey.PasswordPage PasswordPage;
        public Qrs.Mobile.App.Account.NewKey.BackupImagePage BackupImagePage;
        public Qrs.Mobile.App.Account.NewKey.CongratulationsPage CongratulationsPage;
        // Login flow
        public Qrs.Mobile.App.Main.MainPage MainAppPage;
        public Qrs.Mobile.App.Main.ScanPage ScanPage;
        public Qrs.Mobile.App.Main.ScanConfirmPage ScanConfirmPage;
        public Qrs.Mobile.App.Main.PairingPage PairingPage;
        public Qrs.Mobile.App.Main.PairingConfirm PairingConfirmPage;
        public Qrs.Mobile.App.Main.ScanInputPage ScanInputPage; // debug only
        public Qrs.Mobile.App.Main.ActionsPage ActionsPage;
        public Qrs.Mobile.App.Main.SettingsPage SettingsPage;
        // Restore Existing Key flow
        public Qrs.Mobile.App.Account.ExistingKey.BackupScanPage ExBackupScanPage;
        public Qrs.Mobile.App.Account.ExistingKey.BackupPasswordPage ExBackupPasswordPage;
        public Qrs.Mobile.App.Account.ExistingKey.CongratulationsPage ExCongratulationsPage;
        // Create Backup flow
        public Qrs.Mobile.App.Account.CreateBackup.BackupPasswordPage CbBackupPasswordPage;
        public Qrs.Mobile.App.Account.CreateBackup.BackupImagePage CbBackupImagePage;
        public Qrs.Mobile.App.Account.CreateBackup.CongratulationsPage CbCongratulationsPage;

        public Qrs.Mobile.Client.Context.QrsAppContext QrsAppContext;

        public App()
        {
            SetTheme(); // Dark or Light, depending on OS settings
            InitializeComponent();

            // Do NOT initialize the context here - it accesses SecureStorage and may generate keys,
            // which is too slow for the UI thread and gets the app killed by the iOS startup watchdog.
            // A placeholder page is used so that a root view controller exists at the end of launch.
            MainPage = new ContentPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            if (OperatingSystem.IsWindows())
            {            
                const int newheight = 800; //600;
                const int newwidth = 450;

                var window = new Window(MainPage);

                //window.Height = window.MinimumHeight = window.MaximumHeight = newheight;
                //window.Width = window.MinimumWidth = window.MaximumWidth = newwidth;

                window.Height = newheight;
                window.Width = newwidth;

                return window;
            }
            else
                return base.CreateWindow(activationState);
        }


        public void SetTheme()
        {
            AppTheme currentTheme = Application.Current.RequestedTheme;
        }

        public void PrepareWelcomePage()
        {
            WelcomePage ??= new Account.WelcomePage();
        }

        /// <summary>
        /// Starts a brand new navigation stack with a brand new Welcome page as its root.
        /// All the cached pages (from all the flows) are discarded first, b/c they may still be attached
        /// to the previous NavigationPage - re-using them would throw "page already has a parent" later.
        /// </summary>
        public void GotoWelcomePage()
        {
            ResetAllPages();
            PrepareWelcomePage();
            MainPage = new NavigationPage(WelcomePage);
        }

        /// <summary>
        /// Discards the pages of the Welcome / New Key / Restore Existing Key flows.
        /// Called once these flows have completed and the app moves on to the MainAppPage.
        /// </summary>
        public void ResetAccountPages()
        {
            WelcomePage = null;
            // New Key flow
            PasswordPage = null;
            BackupImagePage = null;
            CongratulationsPage = null;
            // Restore Existing Key flow
            ExBackupScanPage = null;
            ExBackupPasswordPage = null;
            ExCongratulationsPage = null;
        }

        /// <summary>
        /// Discards every cached page instance (and the NavigationPage) from all the flows that may have been
        /// initialized during this app lifecycle, so they get re-created (without a parent) on next use.
        /// </summary>
        public void ResetAllPages()
        {
            NavPage = null;
            ResetAccountPages();
            // Login flow
            MainAppPage = null;
            ScanPage = null;
            ScanConfirmPage = null;
            PairingPage = null;
            PairingConfirmPage = null;
            ScanInputPage = null;
            ActionsPage = null;
            SettingsPage = null;
            // Create Backup flow
            CbBackupPasswordPage = null;
            CbBackupImagePage = null;
            CbCongratulationsPage = null;
        }
        public void PrepareNewKeyPages()
        {
            PasswordPage ??= new Account.NewKey.PasswordPage();
            BackupImagePage ??= new Account.NewKey.BackupImagePage();
            CongratulationsPage ??= new Account.NewKey.CongratulationsPage();
        }

        public void PrepareMainPages()
        {            
            MainAppPage ??= new Main.MainPage();
            NavPage ??= new NavigationPage(MainAppPage);
            ActionsPage ??= new Main.ActionsPage();
        }

        public void GotoMainPage()
        {
            PrepareMainPages();
            MainPage = NavPage;
        }

        public void PrepareSettingsPages()
        {
            SettingsPage ??= new Main.SettingsPage();
        }

        public void PrepareLoginPages()
        {
            ScanPage ??= new Main.ScanPage();
            ScanConfirmPage ??= new Main.ScanConfirmPage();
            // debug only:
            ScanInputPage ??= new Main.ScanInputPage();
        }

        public void PrepareScanConfirmPage()
        {
            ScanConfirmPage ??= new Main.ScanConfirmPage();
        }

        public void PreparePairingPage()
        {
            PairingPage ??= new Main.PairingPage();
            PairingConfirmPage ??= new Main.PairingConfirm();
        }

        public void PrepareExistingKeyPages()
        {
            ExBackupScanPage ??= new Account.ExistingKey.BackupScanPage();
            ExBackupPasswordPage ??= new Account.ExistingKey.BackupPasswordPage();
            ExCongratulationsPage ??= new Account.ExistingKey.CongratulationsPage();
        }

        public void PrepareCreateBackupPages()
        {
            CbBackupPasswordPage ??= new Account.CreateBackup.BackupPasswordPage();
            CbBackupImagePage ??= new Account.CreateBackup.BackupImagePage();
            CbCongratulationsPage ??= new Account.CreateBackup.CongratulationsPage();
        }



        protected override async void OnStart()
        {
            // OnStart is 'async void', so any unhandled exception here would terminate the app
            // without a usable stack trace - handle everything explicitly.
            try
            {
                // Create Context!!!
                QrsAppContext = new Qrs.Mobile.Client.Context.QrsAppContext(
                    new MobileDeviceImpl(),
                    new PrefCookieProvider()
                    );

                // Init Context off the UI thread
                await QrsAppContext.InitAsync();
            }
            catch (Exception ex)
            {
                LogDebug($"App context initialization failed: {ex}");
                // fall through to the Welcome flow - the user can re-create/restore the key
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (QrsAppContext?.OrangeKeys == null)
                {
                    // no Keys, start with the Welcome Flow
                    GotoWelcomePage();
                }
                else
                {
                    // have Keys, start with the Login Flow (Scan Flow)
                    PrepareLoginPages();
                    // goto Scan Page
                    MainPage = ScanPage;
                }
            });
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            Debug.WriteLine($"{DateTime.Now} - {message}");
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
