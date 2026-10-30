using System.Collections.Generic;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls;

namespace Qrs.Mobile.App.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AlertsView : ContentView
    {
        public static readonly BindableProperty AlertItemsProperty =
            BindableProperty.Create(nameof(AlertItems), typeof(List<string>), typeof(AlertsView));

        public List<string> AlertItems
        {
            get { return (List<string>)GetValue(AlertItemsProperty); }
            set { SetValue(AlertItemsProperty, value); }
        }

        public AlertsView()
        {
            InitializeComponent();
        }
    }
}
