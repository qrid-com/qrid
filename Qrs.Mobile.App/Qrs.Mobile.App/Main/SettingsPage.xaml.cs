using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SettingsPage : QrsScrollPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            vwDebugOnlyControls.IsVisible = IsDebug;
            chkIgnoreValidation.IsChecked = Context.SettingsIgnoreRequestValidation;
        }

        private async void Continue_Clicked(object sender, EventArgs e)
        {
            Context.SettingsIgnoreRequestValidation = chkIgnoreValidation.IsChecked;
            await Navigation.PopToRootAsync();
        }

        private async void Label1_Tapped(object sender, EventArgs e)
        {
            chkIgnoreValidation.IsChecked = !chkIgnoreValidation.IsChecked;
        }


    }
}