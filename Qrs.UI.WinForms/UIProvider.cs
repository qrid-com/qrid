using Qrid.Common.Client.Api;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.UI.WinForms
{
    public class UIProvider : IUserInteraction
    {
        QrCodeWindow qrWindow = null;
        Form mainForm => Application.OpenForms[0];

        public Action? OnCancelRequested { get; set; }

        public UIProvider()
        {
            //this.qrWindow = qrWindow;
        }

        private void Invoke(Delegate d)
        {
            mainForm.Invoke(d);
        }

        private void BeginInvoke(Delegate d)
        {
            mainForm.BeginInvoke(d);
        }

        public void Invoked_ShowMessage(string message, bool isError)
        {
            LogDebug($"Start - Invoked_ShowMessage({message})");
            // invoked from UI thread
            MessageBox.Show(
                message,
                isError ? "ERROR" : "Message",
                MessageBoxButtons.OK,
                isError ? MessageBoxIcon.Error : MessageBoxIcon.None
                );
            LogDebug($"End   - Invoked_ShowMessage");
        }

        private void LogDebug(string v)
        {
            Debug.WriteLine(v);
        }

        public void Invoked_ShowQrCodeWindow(string sessionId, string qrString)
        {
            // invoked from UI thread

            //var res = qrWindow.ShowDialog(this);

            LogDebug($"Start - Invoked_ShowQrCodeWindow({qrString})");
            if (qrWindow == null)
            {
                qrWindow = new QrCodeWindow();
                qrWindow.OnCancelRequested = () => OnCancelRequested?.Invoke();
            }

            qrWindow.ShowQrCodeWindow(qrString);
            LogDebug($"End   - Invoked_ShowQrCodeWindow");
        }

        public void Invoked_SetDescription(string title, string description)
        {
            // invoked from UI thread

            if (qrWindow == null)
                qrWindow = new QrCodeWindow();

            LogDebug($"Calling SetDescription({title}, {description})");
            qrWindow.SetDescription(title, description);
        }

        public void Invoked_HideQrCodeWindow(string sessionId)
        {
            // invoked from UI thread

            LogDebug($"Start - Invoked_HideQrCodeWindow");
            qrWindow.Hide();
            LogDebug($"End   - Invoked_HideQrCodeWindow");
        }


        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            var pairingCode = InputDialog.Show("Enter the 3 characters Pairing Code from mobile app:", "Pairing");
            if (string.IsNullOrWhiteSpace(pairingCode))
                return CommonContext.ErrorStringResponse("Pairing has been cancelled by user");
            else
                return CommonContext.SuccessStringResponse(pairingCode);
        }

        void IUserInteraction.HideMessageWindow()
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.HideQrCodeWindow(string sessionId)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_HideQrCodeWindow(sessionId);
                }
            );
        }

        void IUserInteraction.ShowError(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowMessage(message, true);
                }
            );
        }

        void IUserInteraction.ShowMessage(string message)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowMessage(message, false);
                }
            );
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.BeginInvoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowQrCodeWindow(sessionId, qrString);
                }
            );

        }

        async Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            // this is called from non-UI thread
            // so cannot touch any UI controls here
            // needs to .Invoke the corresponding delegate from UI thread

            this.BeginInvoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_ShowQrCodeWindow(sessionId, qrString);
                }
            );

        }

        void IUserInteraction.ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.SetDescription(string title, string subtitle)
        {
            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_SetDescription(title, subtitle);
                }
            );
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            this.Invoke
            (
                (MethodInvoker)delegate ()
                {
                    Invoked_SetTunnelServiceUri(tunnelServiceUri);
                }
            );
            
        }

        private void Invoked_SetTunnelServiceUri(string tunnelServiceUri)
        {
            qrWindow.SetTunnelServiceUri($"Tunnel Service : {tunnelServiceUri}");
        }
    }
}
