using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Account.CreateBackup
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CongratulationsPage : QrsScrollPage
    {
        public CongratulationsPage()
        {
            InitializeComponent();
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            App.PrepareMainPages();
            await Navigation.PopToRootAsync();
        }
    }
}