using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Account.NewKey
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CongratulationsPage : QrsScrollPage
    {
        public CongratulationsPage()
        {
            InitializeComponent();
        }

        internal void ResetCheckboxes()
        {
            chkBox1.IsChecked = false;
            chkBox2.IsChecked = false;
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {

            if (!chkBox1.IsChecked ||  !chkBox2.IsChecked)
            {
                await DisplayAlert("Checkboxes unchecked", "Check the checkboxes to confirm your understanding of the Backup process", "Ok");
                return;
            }

            App.NavPage = null; // need to re-create NavPage with new root page as MainAppPage (instead of Welcome page)
            App.PrepareMainPages();
            App.GotoMainPage();
            // the Welcome / New Key / Existing Key pages stay attached to the previous NavigationPage - drop them
            App.ResetAccountPages();
        }

        private async void Label1_Tapped(object sender, EventArgs e)
        {
            chkBox1.IsChecked = !chkBox1.IsChecked;
        }

        private async void Label2_Tapped(object sender, EventArgs e)
        {
            chkBox2.IsChecked = !chkBox2.IsChecked;
        }

    }
}