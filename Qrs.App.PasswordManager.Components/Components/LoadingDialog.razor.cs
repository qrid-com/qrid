using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using QRCoder;
using Qrs.App.PasswordManager.Components.Code;


namespace Qrs.App.PasswordManager.Components.Components
{
    public partial class LoadingDialog //: QridComponentBase
    {

        void Submit() => MudDialog.Close(DialogResult.Ok(true));
        void Cancel() => MudDialog.Close(DialogResult.Cancel());


        public void SetMessage(string s)
        {
            Message = s; // DateTime.Now.ToString() + " - " + 
            //StateHasChanged(); // todo: try commenting this out
        }

        

        public void Log(string m)
        {
            Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss.fff")} - {m}");
        }

    }
}
