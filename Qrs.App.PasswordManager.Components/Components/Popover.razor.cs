using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Components.Code;
using Qrs.App.PasswordManager.Client.Models;


namespace Qrs.App.PasswordManager.Components.Components
{
    public partial class Popover //: QridComponentBase
    {

        [Parameter] public bool IsVisible { get; set; }
        [Parameter] public string Title { get; set; }
        [Parameter] public string Message { get; set; }

        public Popover()
        {

        }
    }
}
