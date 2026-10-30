using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Components.Code;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.App.PasswordManager.Client.Proto;

namespace Qrs.App.PasswordManager.Components.Components
{

    

    public partial class SecretView
    {
          
        
        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            //Message = ResourceItem?.ResourceInfo;
        }
        
    }
}
