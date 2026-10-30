using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PairingPage : QrsScrollPage
    {

        public PairingPage()
        {
            InitializeComponent();
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            App.MainPage = App.PairingConfirmPage;
        }

        private async void Cancel_Clicked(object sender, EventArgs e)
        {
            App.GotoMainPage();
        }

    }
}