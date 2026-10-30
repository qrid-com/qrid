using Qrid.Common.Client.Api;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.TestConsole.HeadlessApps.CA;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.IntegrationTests
{
    public class HeadlessUI : IUserInteraction
    {
        private string _username;
        private string _domain;

        public string FullUserName => $"{_username}@{_domain}";
        public string Username => _username;
        public string Domain => _domain;
        public string AppName => ParentApp.ClientContext.AppName;

        // number of times a QR code has been displayed to the user in this app session
        // (used by the tests to verify whether an operation required a round trip to the QRS Mobile App)
        public int QrCodeShownCount { get; private set; }

        public string UiPairingCode { get; set; } // for Pairing with MA - MA will put here Pairing Code if in the pairing flow

        public ConcurrentBag<TaskCompletionSource<string>> AwaitingScannerTasks { get; set; }

        public ClientAppBase ParentApp { get; set; } // CA - Client App whose context has been created using this UI

        public HeadlessUI(ClientAppBase parentApp, string username, string domain)
        {
            ParentApp = parentApp;
            _username = username;
            _domain = domain;
            AwaitingScannerTasks = new ConcurrentBag<TaskCompletionSource<string>>();
        }

        public void LogDebug(string message)
        {
            ParentApp.LogDebug(message);
        }

        public async Task<HttpResponseModel<StringResponse>> GetUserInputAsync(string inputContext)
        {
            LogDebug($"GetUserInputAsync(inputContext={inputContext})");

            // QRS uses this method only in PAIRING FLOW

            int i = 0;
            while (string.IsNullOrWhiteSpace(UiPairingCode) && i < 10)
            {
                await Task.Delay(500);
                i++;
            }

            if (string.IsNullOrWhiteSpace(UiPairingCode))
                throw new ApplicationException("Pairing Code has not been set! Bug in the testing code????");

            LogDebug($"Pairing Code={UiPairingCode}");

            var res = CommonContext.SuccessStringResponse(UiPairingCode);
            UiPairingCode = null;
            return res;
        }

        public void HideMessageWindow()
        {
            LogDebug($"HideMessageWindow()");
        }

        public void HideQrCodeWindow(string sessionId)
        {
            LogDebug($"HideQrCodeWindow()");
        }

        public void SetDescription(string title, string subtitle)
        {
            LogDebug($"SetDescription(title={title}, subtitle={subtitle})");
        }

        public void SetTunnelServiceUri(string tunnelServiceUri)
        {
            LogDebug($"SetTunnelServiceUri(tunnelServiceUri={tunnelServiceUri})");
        }

        public void ShowError(string message)
        {
            LogDebug($"ERROR - ShowError(message={message})");
        }

        public void ShowMessage(string message)
        {
            LogDebug($"ShowMessage(message={message})");
        }

        public void ShowMessageWindow(string title)
        {
            LogDebug($"ShowMessageWindow(title={title})");
        }

        public void ShowQrCodeWindow(string sessionId, string qrString)
        {
            LogDebug($"ShowQrCodeWindow(qrString={qrString})");
            QrCodeShownCount++;
            CompleteScannerTasks(qrString);
        }

        public Task ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            LogDebug($"ShowQrCodeWindowAsync(qrString={qrString})");
            QrCodeShownCount++;
            CompleteScannerTasks(qrString);
            return Task.CompletedTask;
        }

        private void CompleteScannerTasks(string qrString)
        {
            foreach (var tcs in AwaitingScannerTasks)
            {
                tcs.TrySetResult(qrString);
            }
            AwaitingScannerTasks = new ConcurrentBag<TaskCompletionSource<string>>();
        }

        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            LogDebug($"ShowUsernameWindow()");
            continueWithUsername(FullUserName);
        }
    }
}
