using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;

namespace Qrs.Mobile.App
{
    
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class QrsScrollPage : QrsPageBase
    {
        public QrsScrollPage()
        {
            InitializeComponent();
        }

        public IList<IView> Body => BodyView.Children;
        public IList<IView> Footer => FooterView.Children;

        //public bool IsFooterVisible => FooterView.IsVisible;

        public static readonly BindableProperty IsFooterVisibleProperty =
            BindableProperty.Create("IsFooterVisible", typeof(bool), typeof(QrsScrollPage), false, 
                defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnIsFooterVisibleChanged);

        public bool IsFooterVisible
        {
            get => (bool)GetValue(IsFooterVisibleProperty);
            set => SetValue(IsFooterVisibleProperty, value);
        }

        public static void OnIsFooterVisibleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            Console.WriteLine("==================== OnIsFooterVisibleChanged ============");
            if (bindable != null && bindable is QrsScrollPage basePage)
            {
                basePage.FooterView.IsVisible = (bool)newValue;
            }
        }

        //
        // IS LOADING - show busy indicator while loading data etc
        //

        public Controls.PopupLoadingControl PopupLoadingView = null;

        public static readonly BindableProperty IsLoadingProperty =
            BindableProperty.Create("IsLoading", typeof(bool), typeof(QrsScrollPage), false,
                defaultBindingMode: BindingMode.TwoWay, propertyChanged: OnIsLoadingChanged);


        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static void OnIsLoadingChanged(BindableObject bindable, object oldValue, object newValue)
        {
            //Console.WriteLine("=================  ===  OnIsLoadingChanged  ===  =========");
            if (bindable != null && bindable is QrsScrollPage basePage)
            {
                //Console.WriteLine("=================  ===  inside IF  ===  =========");
                basePage.CreatePopupLoadingView();
                basePage.PopupLoadingView.IsVisible = (bool)newValue;
            }
        }

        public void CreatePopupLoadingView()
        {
            if (PopupLoadingView == null)
            {
                PopupLoadingView = new Controls.PopupLoadingControl();
                this.RootGrid.Children.Add(PopupLoadingView);
            }
        }



    }
}