using Qrid.Common.Crypto;
using Qrid.Common.Http;
using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Account
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class WelcomePage : QrsScrollPage
    {

        public string AppVersion2 { get { return AppVersion; } }
        public WelcomePage()
        {
            InitializeComponent();
        }

        private async void CreateNewAccount_Clicked(object sender, EventArgs e)
        {
            App.PrepareNewKeyPages();
            var rnd3 = CryptoUtils.GetRandomBytesToStringLength(3).ToUpper();
            var keyName = $"MasterKey_{DateTime.Now.ToString("yyyyMMdd")}_{rnd3}";
            App.PasswordPage.SetKeyName(keyName);
            await Navigation.PushAsync(App.PasswordPage, false);
        }

        private async void UseExistingAccount_Clicked(object sender, EventArgs e)
        {
            App.PrepareExistingKeyPages();
            await Navigation.PushAsync(App.ExBackupScanPage, false);
        }

        private async void RunTest_Clicked(object sender, EventArgs e)
        {
            //MyTextEdit.Text = DateTime.Now.ToString();

            //Test_NewUserFlow();
        }

        public async void Test_NewUserFlow()
        {
            throw new ApplicationException("not implemented");
        }

        public bool IsError<T>(HttpResponseModel<T> response) where T : Qrid.Common.Proto.BaseData
        {

            //var response = (Qrid.Models.Http.HttpResponseModel<BaseData>)(responseObj);

            if (!response.IsSuccess())
            {
                // error, stay on this page
                DisplayAlert("ERROR", response.Message, "OK");
                return true;
            }
            return false;
        }



    }
}