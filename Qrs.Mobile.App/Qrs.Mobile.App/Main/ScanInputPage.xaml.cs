using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanInputPage : QrsScrollPage
    {
        public ScanInputPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            QrCodeString.Text = string.Empty;
            base.OnAppearing();
            
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }

        
        private async void Continue_Clicked(object sender, EventArgs e)
        {
            var text = QrCodeString.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                Context.QrCommandFlow.ManualQrCodeString = string.Empty;
                App.MainPage = App.NavPage;
            }
            else
            {
                if (text.StartsWith("qrid://"))
                    text = text.Substring(7);
                else if (text.StartsWith("qrs://"))
                    text = text.Substring(6);

                Context.QrCommandFlow.ManualQrCodeString = text;
                App.MainPage = App.ScanPage;
            }
        }

    }
}