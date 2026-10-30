using Qrs.App.PasswordManager.Components.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code.ViewModels
{

    public class MudInputVm : ViewModelBase
    {
        // represents collection of properties that could be used to render MudBlazor input elements

        public string Text { get; set; }
        public bool Visible { get; set; }
        public bool ReadOnly { get; set; }
        public bool Disabled { get; set; }
        public MudBlazor.Variant Variant { get; set; }






    }
}
