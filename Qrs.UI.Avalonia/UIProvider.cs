using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Qrid.Common.Client.Api;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using System.Diagnostics;

namespace Qrs.UI.Avalonia
{
    public class UIProvider : IUserInteraction
    {
        private QrCodeWindow? qrWindow;

        private Window? MainWindow
        {
            get
            {
                if (global::Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                    return desktop.MainWindow;
                return null;
            }
        }

        public UIProvider()
        {
        }

        public void Invoked_ShowMessage(string message, bool isError)
        {
            LogDebug($"Start - Invoked_ShowMessage({message})");
            var owner = MainWindow;
            if (owner != null)
            {
                var dialog = MsBox.Avalonia.MessageBoxManager.GetMessageBoxStandard(
                    isError ? "ERROR" : "Message",
                    message,
                    MsBox.Avalonia.Enums.ButtonEnum.Ok,
                    isError ? MsBox.Avalonia.Enums.Icon.Error : MsBox.Avalonia.Enums.Icon.None,
                    WindowStartupLocation.CenterOwner);
                dialog.ShowWindowDialogAsync(owner).GetAwaiter().GetResult();
            }
            LogDebug($"End   - Invoked_ShowMessage");
        }

        private void LogDebug(string v)
        {
            Debug.WriteLine(v);
        }

        public void Invoked_ShowQrCodeWindow(string sessionId, string qrString)
        {
            LogDebug($"Start - Invoked_ShowQrCodeWindow({qrString})");
            if (qrWindow == null)
                qrWindow = new QrCodeWindow();

            qrWindow.ShowQrCodeWindow(qrString);
            LogDebug($"End   - Invoked_ShowQrCodeWindow");
        }

        public void Invoked_SetDescription(string title, string description)
        {
            if (qrWindow == null)
                qrWindow = new QrCodeWindow();

            LogDebug($"Calling SetDescription({title}, {description})");
            qrWindow.SetDescription(title, description);
        }

        public void Invoked_HideQrCodeWindow(string sessionId)
        {
            LogDebug($"Start - Invoked_HideQrCodeWindow");
            qrWindow?.Hide();
            LogDebug($"End   - Invoked_HideQrCodeWindow");
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            string? pairingCode = null;
            var owner = MainWindow;
            if (owner != null)
            {
                pairingCode = await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    return await InputDialog.Show(
                        "Enter the 3 characters Pairing Code from mobile app:",
                        "Pairing",
                        owner);
                });
            }

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
            Dispatcher.UIThread.Invoke(() =>
            {
                Invoked_HideQrCodeWindow(sessionId);
            });
        }

        void IUserInteraction.ShowError(string message)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Invoked_ShowMessage(message, true);
            });
        }

        void IUserInteraction.ShowMessage(string message)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Invoked_ShowMessage(message, false);
            });
        }

        void IUserInteraction.ShowMessageWindow(string title)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.ShowQrCodeWindow(string sessionId, string qrString)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Invoked_ShowQrCodeWindow(sessionId, qrString);
            });
        }

        async Task IUserInteraction.ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            Dispatcher.UIThread.Post(() =>
            {
                Invoked_ShowQrCodeWindow(sessionId, qrString);
            });
        }

        void IUserInteraction.ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.SetDescription(string title, string subtitle)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Invoked_SetDescription(title, subtitle);
            });
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                Invoked_SetTunnelServiceUri(tunnelServiceUri);
            });
        }

        private void Invoked_SetTunnelServiceUri(string tunnelServiceUri)
        {
            if (qrWindow == null)
                qrWindow = new QrCodeWindow();

            qrWindow.SetTunnelServiceUri($"Tunnel Service : {tunnelServiceUri}");
        }
    }
}
