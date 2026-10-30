using Qrid.Common.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Qrid.Common.Client.UI
{
    public interface IUserInteraction
    {
        void ShowUsernameWindow(Action<string> continueWithUsername);
        void ShowQrCodeWindow(string sessionId, string qrString);
        Task ShowQrCodeWindowAsync(string sessionId, string qrString);
        void HideQrCodeWindow(string sessionId);

        void ShowMessageWindow(string title);
        void ShowMessage(string message);
        void ShowError(string message);

        Task<HttpResponseModel<StringResponse>> GetUserInputAsync(string inputContext);

        //void SetStatus(string status);

        void HideMessageWindow();

        void SetDescription(string title, string subtitle);

        void SetTunnelServiceUri(string tunnelServiceUri);

    }
}
