using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Qrs.App.PasswordManager.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class QridLayoutBase : LayoutComponentBase 
    {
        [Inject] 
        public ApplicationService App { get; set; }

        [Inject]
        public ILogger<QridLayoutBase> Logger { get; set; }


        public PwmContext Context => App.Context;

        
        public void StateChanged()
        {
            this.StateHasChanged();
        }
        public void LogInfo(string message)
        {
            App.LogInfo(message);
        }
    }

}
