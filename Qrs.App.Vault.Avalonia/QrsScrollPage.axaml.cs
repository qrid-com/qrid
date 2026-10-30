using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Input;
using Avalonia.Interactivity;




namespace Qrs.App.Vault.Avalonia
{

    
    public partial class QrsScrollPage : QrsPageBase
    {
        public QrsScrollPage()
        {
            InitializeComponent();

        }

        public string StatusText { get => lblStatus.Text; set => lblStatus.Text = value; }
       

        //public bool IsFooterVisible => FooterView.IsVisible;


        //
        // IS LOADING - show busy indicator while loading data etc
        //

        




    }
}