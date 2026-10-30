using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Data;
using Qrid.Common.Http;
using Qrs.Mobile.Client.Context;
using Qrs.Mobile.Client.Device;
using System;
using System.Threading.Tasks;

namespace Qrs.TestConsole.LegacyTests
{
    public class TestBase : IUserInteraction
    {
        
        public QrsAppContext MaContext;
        public Qrs.Client.Context.ClientContext CaContext;

        public string UiSessionId;
        public string UiQrString;
        public string UiPairingCode;

        public void Init(IDevice device, Qrs.Client.Context.ClientContext clientContext)
        {
            MaContext = new QrsAppContext(device, new MemoryCookieProvider());
            var initResult = MaContext.Init();
            EnsureOrangeKeys();
            

            CaContext = clientContext;
        }

        public void LogDebug(string message)
        {
            var text = $"{DateTime.Now.ToString()} - {message}";
            System.IO.File.AppendAllText(@"C:\Users\alex\Desktop\TestLog.txt", text + Environment.NewLine);
            Console.WriteLine(text);
        }

        public void DisplayAlert(string caption, string message, string button)
        {
            LogDebug($"{caption}: {message}");
        }

        public bool IsError<T>(HttpResponseModel<T> response) where T : Qrid.Common.Proto.BaseData
        {
            if (!response.IsSuccess())
            {
                // error, stay on this page
                DisplayAlert("ERROR", response.Message, "OK");
                return true;
            }
            return false;
        }

        #region UI Interface implementation
        public virtual void ShowQrCodeWindow(string sessionId, string qrString)
        {
            UiSessionId = sessionId;
            UiQrString = qrString;
        }

        public async virtual Task ShowQrCodeWindowAsync(string sessionId, string qrString)
        {
            UiSessionId = sessionId;
            UiQrString = qrString;
        }

        public virtual void HideQrCodeWindow(string sessionId)
        {
            LogDebug("called UI.HideQrCodeWindow()");
        }

        public virtual void ShowMessage(string message)
        {
            LogDebug($"called UI.ShowMessage('{message}')");
        }

        public virtual void ShowError(string message)
        {
            LogDebug($"called UI.ShowError('{message}')");
        }
        public void ShowMessageWindow(string title)
        {
            throw new NotImplementedException();
        }

        public void HideMessageWindow()
        {
            throw new NotImplementedException();
        }

        public void ShowUsernameWindow(Action<string> continueWithUsername)
        {
            throw new NotImplementedException();
        }

        void IUserInteraction.SetDescription(string title, string description)
        {
            LogDebug($"called UI.SetDescription(title='{title}', description='{description}')");
        }

        void IUserInteraction.SetTunnelServiceUri(string tunnelServiceUri)
        {
            LogDebug($"called UI.SetTunnelServiceUri(tunnelServiceUri='{tunnelServiceUri}')");
        }

        #endregion

        private async Task PerformLoginSubTask()
        {
            throw new ApplicationException("not implemented");
            //// old code:
            //var LoginTask = CaContext.WrapKeyAsync(null,null);
            //await LoginTask;
        }

        public async Task<bool> IsServiceUp(int waitSeconds)
        {
            var exitTime = DateTime.Now.AddSeconds(waitSeconds);
            while (DateTime.Now < exitTime)
            {
                var res = await CaContext.TunnelClient.IsServiceUp();
                if (res)
                    return true;
                else
                    await Task.Delay(1000);
            }
            return false;
        }

        public async Task EnsureServiceUp(int waitSeconds = 60)
        {
            var isUp = await IsServiceUp(waitSeconds);
            if (!isUp) 
                throw new ApplicationException($"Cannot continue - Tunnel Service is down for more than {waitSeconds} seconds");
        }

        public async Task PerformMobileQrCodeScan()
        {

            await Task.Delay(500);

            // ===============================================================
            // MA - Blind Login - executing in mobile app
            var qrString = await GetQrStringAsync();
            
            // parse QR code data
            var qrCmd = QrCommand.Create(qrString);

            // Download Request
            await MaContext.DownloadRequestAsync(qrCmd);

            if (MaContext.QrCommandFlow.HasError())
                LogThrow(new ApplicationException(MaContext.QrCommandFlow.ErrorMessage));

            if (MaContext.QrCommandFlow.HasPairing())
            {
                // pair
                UiPairingCode = MaContext.QrCommandFlow.PairingRequest.PairingCode;

                // await payload after pairing
                await MaContext.DownloadRequestAfterPairingAsync();
            }

            if (!MaContext.QrCommandFlow.HasPayload())
                LogThrow(new ApplicationException("Unexpected state - no pairing, no error, no payload"));
        }

        protected void LogThrow(Exception ex)
        {
            LogDebug($"ERROR - - - {ex.Message}");
            throw ex;
        }

        protected void LogThrow(string exceptionMessage)
        {
            LogDebug($"ERROR - LogThrow(string) - - - {exceptionMessage}");
            throw new ApplicationException(exceptionMessage);
        }

        private bool IsQrCodeReady()
        {
            return (false == string.IsNullOrWhiteSpace(UiQrString)); // && ClientContext.QrCommandStatus == Client.Context.QrCommandStatus.ServerSessionStarted);
        }

        private async Task<string> GetQrStringAsync()
        {
            var timeoutDate = DateTime.Now.AddSeconds(20);

            while(!IsQrCodeReady() && DateTime.Now <= timeoutDate)
            {
                await Task.Delay(500);
                //System.Threading.Thread.Sleep(500);
            }

            if (!IsQrCodeReady())
                LogThrow(new ApplicationException("timeout while scanning QR code - IsQrCodeReady() keeps on returning false"));

            return UiQrString;
        }

        async Task<HttpResponseModel<StringResponse>> IUserInteraction.GetUserInputAsync(string inputContext)
        {
            int i = 0;
            while (string.IsNullOrWhiteSpace(UiPairingCode) && i < 10)
            {
                await Task.Delay(500);
                i++;
            }
            
            if (string.IsNullOrWhiteSpace(UiPairingCode))
                throw new ApplicationException("Pairing Code has not been set! Bug in the testing code????");

            var res = CommonContext.SuccessStringResponse(UiPairingCode);
            UiPairingCode = null;
            return res;
        }

        void EnsureOrangeKeys()
        {
            if (MaContext.OrangeKeys == null)
            {
                _ = MaContext.NewMasterKeyAsync("test_password", "test_masterkey").Result;
            }
        }
    }
}
