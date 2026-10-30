using System;
using Microsoft.Maui.Controls.Xaml;

namespace Qrs.Mobile.App
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TestPage : QrsScrollPage
    {
        public TestPage()
        {
            InitializeComponent();
        }

        

        
        private async void Continue_Clicked(object sender, EventArgs e)
        {
            
        }

    }
}