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
    public partial class PopupLoadingControl : ContentView
    {

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(TitleView));
        
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }
        
        
        public PopupLoadingControl()
        {
            InitializeComponent();
        }
    }
}