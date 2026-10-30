using Qrs.Mobile.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MainPage : QrsScrollPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            SetKeyName(Context.OrangeKeys?.KeyName ?? "empty - no key");
            base.OnAppearing();
        }

        private async void Scan_Clicked(object sender, EventArgs e)
        {
            App.PrepareLoginPages();
            App.MainPage = App.ScanPage;
        }

        private async void Actions_Clicked(object sender, EventArgs e)
        {
            // got System.InvalidOperationException: Page must not already have a parent.
            await Navigation.PushAsync(App.ActionsPage);
        }


        private async void BtnKey_Clicked(object sender, EventArgs e)
        {

        }

        internal void SetKeyName(string keyName)
        {
            BtnKey.Text = keyName;
        }
    }
}