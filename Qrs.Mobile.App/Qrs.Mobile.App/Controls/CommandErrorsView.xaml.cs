using Qrs.Mobile.App.Data;
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
    public partial class CommandErrorsView : ContentView
    {

        public static readonly BindableProperty CommandDataItemsProperty = BindableProperty.Create(nameof(CommandDataItems), typeof(List<CommandDataItem>), typeof(CommandErrorsView));
        
        public List<CommandDataItem> CommandDataItems
        {
            get { return (List<CommandDataItem>)GetValue(CommandDataItemsProperty); }
            set 
            { 
                SetValue(CommandDataItemsProperty, value);
            }
        }
        
        
        public CommandErrorsView()
        {
            InitializeComponent();

            //CommandDataItems = new List<CommandDataItem>
            //{
            //    new CommandDataItem { Key = "Key Operation", Value = "UNWRAP (your key so it could be used to READ/WRITE your DATA)" },
            //    new CommandDataItem { Key = "Key Description", Value = "File Key for \"My_Clients\\2242b08c-1de7-4792-88a5-59350f8d5df2.qrs2\"" },
            //    new CommandDataItem { Key = "Resource Name", Value = "godaddy.com" },
            //    new CommandDataItem { Key = "Username", Value = "alexk123" }
            //};

            //CommandDataItems = null;
        }
    }
}