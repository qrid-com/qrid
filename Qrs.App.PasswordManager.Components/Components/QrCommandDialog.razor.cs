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
    public partial class QrCommandDialog //: QridComponentBase
    {

        public void Submit() => MudDialog.Close(DialogResult.Ok(true));
        public void CancelClick()
        {
            MudDialog.Close(DialogResult.Cancel());
            App.PerformCancelQrCodePolling();
        }

        public async void BackClick()
        {
            // go back to the Login - Page 1 (aka Username)
            DisplayMode = "username";
            StateChanged();
        }

        public async void NewUserClick()
        {
            // go to New User page
            DisplayMode = "newuser";
            StateChanged();
        }

        public async void ContinueClick()
        {
            // TODO - validate User Input?

            UserInput = UserInput?.Trim().ToLower() ?? String.Empty;


            DisplayMode = "loading";
            StateChanged();

            App.ContinueWithUserInput(UserInput);

            //StateChanged();
        }

        public void SetQrCodeString(string s)
        {
            DisplayMode = "qrcode";
            QrCodeString = s; // DateTime.Now.ToString() + " - " + 
            StateChanged(); // todo: try commenting this out
        }

        public void StartUserInputMode()
        {
            DisplayMode = "userinput";
            UserInput = string.Empty; // DateTime.Now.ToString() + " - " + 
            StateChanged();
        }

    }
}
