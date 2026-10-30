using Qrid.Common.Crypto;
using Qrid.Common.Proto;
using Qrs.Mobile.Client;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Google.Protobuf;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActionsPage : QrsScrollPage
    {
        public ActionsPage()
        {
            InitializeComponent();
            vwDebugOnlyControls.IsVisible = IsDebug;
        }

        

        private async void Logout_Clicked(object sender, EventArgs e)
        {
            await Logout();
        }

        private async void Backup_Clicked(object sender, EventArgs e)
        {
            App.PrepareCreateBackupPages();
            if (App.CbBackupPasswordPage.Parent != null)
            {
                await DisplayPromptAsync("Error", $"CbBackupPasswordPage already has parent: {App.CbBackupPasswordPage.Parent}");
                return;
            }

            App.CbBackupPasswordPage.SetKeyName(Context.OrangeKeys.KeyName);

            await Navigation.PushAsync(App.CbBackupPasswordPage);

        }

        private async void ScanInput_Clicked(object sender, EventArgs e)
        {
            App.PrepareLoginPages();
            App.MainPage = App.ScanInputPage;

        }

        private async void Settings_Clicked(object sender, EventArgs e)
        {
            App.PrepareSettingsPages();
            await Navigation.PushAsync(App.SettingsPage);
        }

        private async void ResetPairing_Clicked(object sender, EventArgs e)
        {
            IsLoading = true;

            await Task.Run(() => Context.ResetPairing());

            IsLoading = false;
        }

        private async void ResetKnownServers_Clicked(object sender, EventArgs e)
        {
            IsLoading = true;

            await Task.Run(() => MobUtils.ClearKnownServers());

            IsLoading = false;
        }

        private async void Test_Clicked(object sender, EventArgs e)
        {
            //var testPage = new TestPage();
            //await Navigation.PushAsync(testPage);

            IsLoading = true;

            //await Task.Delay(2000); 



            await TestRegex();

            
            IsLoading = false;

        }

        private async Task TestRegex()
        {
            var x1 = Context.QrCommandFlow.IsCanonicalAppName("JaVA");
            var x2 = Context.QrCommandFlow.IsCanonicalAppName("JAVA");
            var x3 = Context.QrCommandFlow.IsCanonicalAppName("JAVA-COFFEE");
            var x4 = Context.QrCommandFlow.IsCanonicalAppName("JAVA COFFEE");
            var x5 = Context.QrCommandFlow.IsCanonicalAppName("JAVA  COFFEE");
            var x6 = Context.QrCommandFlow.IsCanonicalAppName(" JAVA");
            var x7 = Context.QrCommandFlow.IsCanonicalAppName("JAVA ");
            var x8 = Context.QrCommandFlow.IsCanonicalAppName("-JAVA");
            var x9 = Context.QrCommandFlow.IsCanonicalAppName("JAVA-");
            var x10 = Context.QrCommandFlow.IsCanonicalAppName("");
            var x11 = Context.QrCommandFlow.IsCanonicalAppName(" ");
        }

        private async Task TestAead()
        {
            // generate sym key
            var key = CryptoUtils.GetRandomSymKey();
            var plainText = StringObject.CreateWithString("Alex");
            var ad1 = StringObject.CreateWithString("ad1");
            var ad2 = StringObject.CreateWithString("222");
            // encrypt with ad1
            var encrypted1 = CryptoUtils.SymEncrypt(plainText, key, ad1);
            var encrypted2 = CryptoUtils.SymEncrypt(plainText, key, ad2);
            // decrypt with ad1
            var decrypted1 = CryptoUtils.SymDecrypt(encrypted1, key, ad1.ToByteArray());
            var plainText1 = StringObject.Create(decrypted1);
            Console.WriteLine(plainText1.Value);
            // decrypt with ad2
            var decrypted2 = CryptoUtils.SymDecrypt(encrypted1, key, ad2.ToByteArray());
            var plainText2 = StringObject.Create(decrypted2);
            Console.WriteLine(plainText2.Value);
        }


    }
}