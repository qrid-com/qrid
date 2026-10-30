using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Qrs.Mobile.App.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class HowItWorks : ContentView
    {
        public string AppVersion => Microsoft.Maui.ApplicationModel.AppInfo.Current.VersionString;

        public HowItWorks()
        {
            InitializeComponent();
        }
    }
}