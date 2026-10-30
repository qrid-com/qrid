using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Account.ExistingKey
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CongratulationsPage : QrsScrollPage
    {
        public string MessageText { get; set; }
        public CongratulationsPage()
        {
            InitializeComponent();
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            App.NavPage = null; // need to re-create NavPage with new root page as MainAppPage (instead of Welcome page)
            App.PrepareMainPages();
            App.GotoMainPage();
            // the Welcome / New Key / Existing Key pages stay attached to the previous NavigationPage - drop them
            App.ResetAccountPages();
        }
    }
}