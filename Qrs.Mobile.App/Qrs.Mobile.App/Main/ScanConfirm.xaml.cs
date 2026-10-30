using Qrs.Proto;
using Qrs.Proto.Extensions;
using Qrs.Mobile.App.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Controls.Xaml;
using Microsoft.Maui.Controls.Compatibility;
using Microsoft.Maui.Controls;
using Microsoft.Maui;
using Qrs.Mobile.Client.Flows;
using Qrs.Proto.Api;
using Qrid.Common.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Qrs.Mobile.App.Main
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ScanConfirmPage : QrsScrollPage
    {
        public bool IsValidAssociatedData = true;
        public bool ValidateAssociatedData = true;
        public string ErrorMessage = string.Empty;

        public ScanConfirmPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var err = Context.QrCommandFlow.ErrorMessage;
            if (!string.IsNullOrWhiteSpace(err))
            {
                // ERROR - show ERROR then goto MainPage
                Device.BeginInvokeOnMainThread(() =>
                {
                    // stay on this page
                    DisplayAlert("ERROR", err, "OK");
                    App.GotoMainPage();
                });
            }
        }

        private async void No_Clicked(object sender, EventArgs e)
        {
            App.GotoMainPage();
            //await Navigation.PushAsync(App.UsernamePage, false);
        }

        private async void Yes_Clicked(object sender, EventArgs e)
        {
            try
            {
                // Save the pending (newly seen) server to known servers before performing the command
                if (!string.IsNullOrWhiteSpace(Context.QrCommandFlow.PendingKnownServer))
                    Qrs.Mobile.Client.MobUtils.AddKnownServer(Context.QrCommandFlow.PendingKnownServer);

                // this may throw some BouncyCastle errors when wrong key is used to decrypt, etc
                // Org.BouncyCastle.Crypto.InvalidCipherTextException: mac check in GCM failed
                await PerformCommand();
            }
            catch (Org.BouncyCastle.Crypto.InvalidCipherTextException bx)
            {
                // UI - show error
                ShowError($"Error while performing a cryptographic operation - may be caused by decrypting with the wrong Master Key. Original error is: {bx.Message}");
                return;
            }
            catch (Exception ex)
            {
                // UI - show error
                ShowError($"{ex.Message}");
                return;
            }
            
            // flow completed - Navigate to Home 
            App.GotoMainPage();
        }

        private async Task PerformCommand()
        {
            var qcmd = Context.QrCommandFlow.QrCommand;
            switch (qcmd.Cmd)
            {
                case "keyop":
                    var keyopResponse = Context.KeyOp();
                    var sendKeyopResponse = await Context.QrCommandSendResponseAsync(keyopResponse);
                    if (!sendKeyopResponse.IsSuccess())
                    {
                        // UI - show error
                        ShowError(sendKeyopResponse.Message);
                        return;
                    }
                    break;

                default:
                    throw new ApplicationException($"QR code contains unsupported cmd='{qcmd.Cmd}'");
            }
        }

        public void DownloadCompletedWithError()
        {
            // hide the Busy Indicator / spinning wheel
            IsLoading = false;

            App.GotoMainPage();
        }

        public void DownloadCompleted()
        {
            // this will be called once Data are Downloaded into Context.QrCommandFlow.Payload

            IsLoading = false;  // hide the Busy Indicator / spinning wheel
            HideLoadingLabel(); // hide "Loading..." label

            if (!Context.QrCommandFlow.HasPayload())
                if (!Context.QrCommandFlow.HasError())
                    // no payload and no error message - weird, should not happen, but let's show "Unexpected Error"
                    Context.QrCommandFlow.ErrorMessage = "Unexpected Error - Payload is missing - cannot continue";

            if (Context.QrCommandFlow.HasError())
            {
                DisplayErrorMessage();
                return;
            }


            // parse payload 
            Context.QrCommandFlow.ParsePayload(Context.OrangeKeys.KeyName);
            
            LogDebug("===== DownloadCompleted - printing QrCommandFlow ======================================");
            LogDebug(Context.QrCommandFlow.ToString());
 

            // Validate Command Request
            Context.QrCommandFlow.ValidateCommand(Context.OrangeKeys.KeyName);
            bool isValid = Context.QrCommandFlow.IsValidCommand();
            // canContinue shows the "Yes" button (that performs the requested operation)
            // canContinue is TRUE only when { NO FATAL ERRORS and (NO VAL ERRORS or IGNORE VAL ERRORS) }
            bool canContinue = (isValid || Context.SettingsIgnoreRequestValidation); // && !Context.QrCommandFlow.HasFatalErrors();

            // title
            ctrTitleView.Title = isValid ? "Please Confirm" : "Invalid Request Data";

            // errors control (content/IsVisible)
            ctrCommandErrorsView.CommandDataItems = GetValidationErrors();
            ctrCommandErrorsView.IsVisible = ctrCommandErrorsView.CommandDataItems.Count > 0;

            // command data control (content/IsVisible)
            if (canContinue)
            {
                // load request data into ctrCommandDataView
                ShowRequestData();
                ctrCommandDataView.IsVisible = true;
            }
            else
                ctrCommandDataView.IsVisible = false;

            // Yes / No buttons
            ShowButtons(canContinue);

            // security alerts about the Tunnel Server URL
            var alerts = Context.QrCommandFlow.Alerts;
            ctrAlertsView.AlertItems = alerts;
            ctrAlertsView.IsVisible = alerts != null && alerts.Count > 0;

        }

        public void AddErrorsExplanation(List<CommandDataItem> data)
        {
            var cdi = new CommandDataItem();
            cdi.Key = $"{Context.QrCommandFlow.ValidationErrors.Count + Context.QrCommandFlow.FatalErrors.Count} errors ({Context.QrCommandFlow.FatalErrors.Count} fatal)";
            cdi.Value = "Please review the validation errors below";
            data.Add(cdi);
        }

        public List<CommandDataItem> GetValidationErrors()
        {
            var res = new List<CommandDataItem>();

            if (Context.QrCommandFlow.FatalErrors.Count + Context.QrCommandFlow.ValidationErrors.Count > 0)
            {
                AddErrorsExplanation(res);

                foreach (var ve in Context.QrCommandFlow.FatalErrors)
                {
                    var cdi = new CommandDataItem();
                    cdi.Key = "Fatal - " + ve.Key;
                    cdi.Value = ve.Value;
                    res.Add(cdi);
                }

                foreach (var ve in Context.QrCommandFlow.ValidationErrors)
                {
                    var cdi = new CommandDataItem();
                    cdi.Key = ve.Key;
                    cdi.Value = ve.Value;
                    res.Add(cdi);
                }
            }

            return res;
        }


        public void ShowRequestData()
        {
            // valid request OR user wants to ignore validation

            var qcmd = Context.QrCommandFlow.QrCommand;
            switch (qcmd.Cmd)
            {
                case "keyop":
                    DisplayKeyOpRequestData();
                    break;
                default:
                    throw new ApplicationException($"QR code contains unsupported cmd='{qcmd.Cmd}'");
            }
        }

        public void ShowButtons(bool isYesVisible = true)
        {
            btnYes.IsVisible = isYesVisible;
            btnNo.IsVisible = true;
            btnNo.Text = isYesVisible ? "NO" : "Cancel";
        }

        public void HideButtons()
        {
            btnYes.IsVisible = false;
            btnNo.IsVisible = false;
        }

        public void DisplayErrorMessage()
        {
            // Hide the "Loading" label
            // populate controls with Data

            // the actual error is in QrCommandFlow.ErrorMessage
            // - will be displayed IN A POPUP by the code in OnAppearing override
            // here we just hide the Yes / No buttons to avoid distraction
            
            var fs = new FormattedString();
            fs.Spans.Add(new Span { Text = "ERROR" });
            
            lblMessage.FormattedText = fs;
            lblMessage.IsVisible = true;

            HideButtons();
            
        }

        private void AddDifference(List<CommandDataItem> commandData, AssociatedData2 oldAd, AssociatedData2 newAd)
        {
            if (oldAd == null || newAd == null) return;

            var cdi = new CommandDataItem();
            var diff = oldAd.DifferenceWith(newAd);
            cdi.Key = "What has changed in AD attributes?";
            cdi.Value = string.IsNullOrWhiteSpace(diff) ? "no changes in AD attributes" : diff;
            commandData.Add(cdi);
        }

        private void AddAssociatedData(List<CommandDataItem> commandData, AssociatedData2 associatedData)
        {
            if (associatedData == null)
            {
                var cdi = new CommandDataItem();
                cdi.Key = "Empty Associated Data";
                cdi.Value = "No Associated Data has been provided";
                commandData.Add(cdi);
            }
            else
            {
                foreach( var adi in associatedData.AssociatedDataItems)
                {
                    var cdi = new CommandDataItem();
                    cdi.Key = adi.Key;
                    cdi.Value = adi.Value;
                    commandData.Add(cdi);
                }
            }
        }

        private void AddTunnelServer(List<CommandDataItem> commandData)
        {
            var cdi = new CommandDataItem();
            cdi.Key = "Tunnel Server";
            cdi.Value = Context.TunnelClient.BaseUrl;
            commandData.Add(cdi);
        }

        private void AddAppName(List<CommandDataItem> commandData)
        {
            var appName = Context.QrCommandFlow.KeyOpRequest?.AppName;
            if (string.IsNullOrWhiteSpace(appName))
                // never happens b/c of the preceeding validation requires non-empty AppName in that field
                // so if we are here, it is a bug in MA, fail loudly
                throw new ApplicationException("AppName is missing in the KeyOpRequest - cannot continue");
            

            var cdi = new CommandDataItem();
            cdi.Key = "Application Name";
            cdi.Value = appName;
            commandData.Add(cdi);
        }

        public void DisplayKeyOpRequestData()
        {
            var data = new List<CommandDataItem>();
            AddTunnelServer(data);
            AddAppName(data);
            foreach (var item in Context.QrCommandFlow.RequestItems)
            {
                // TODO - add separators between the items
                AddKeyOpDescription(data, item.KeyOpRequestItem.KeyOp);
                AddDifference(data, item.OldAssociatedData, item.NewAssociatedData); // only if both are not null
                AddAssociatedData(data, item.DisplayAssociatedData);
            }
            ctrCommandDataView.CommandDataItems = data;
        }

        public void AddKeyOpDescription(List<CommandDataItem> data, KeyOp keyOp)
        {
            switch (keyOp)
            {
                case KeyOp.WrapNewPayload:
                    data.Add(new CommandDataItem { Key = "Key Operation", Value = "WRAP (to store your data)" });
                    break;
                case KeyOp.WrapUpdatedPayload:
                    data.Add(new CommandDataItem { Key = "Key Operation", Value = "WRAP (to store your data)" });
                    break;
                case KeyOp.Unwrap:
                    data.Add(new CommandDataItem { Key = "Key Operation", Value = "UNWRAP (to read your data)" });
                    break;
                case KeyOp.Reseal:
                    data.Add(new CommandDataItem { Key = "Key Operation", Value = "RESEAL (to modify associated data describing your key, or to rotate the key)" });
                    break;
                default:
                    // do nothing here
                    break;
            }
        }

        public void HideLoadingLabel()
        {
            var fs = new FormattedString();
            fs.Spans.Add(new Span { Text = "" });
            //fs.Spans.Add(new Span { Text = Environment.NewLine + ad.Title + Environment.NewLine, TextColor = Color.FromHex("#3f77f6"), FontAttributes = FontAttributes.Bold });
            //fs.Spans.Add(new Span { Text = "data by wrapping its key?" });

            lblMessage.FormattedText = fs;
            lblMessage.IsVisible = false;
        }

        public void DownloadStarting()
        {
            // show "Loading" label
            Context.QrCommandFlow.ClearRequest();
            lblMessage.Text = $"Loading...";
            lblMessage.IsVisible = true;
            // show the Busy Indicator / spinning wheel
            IsLoading = true ;
        }

    }
}