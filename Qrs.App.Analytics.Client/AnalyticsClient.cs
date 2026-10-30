using Qrid.Common.Analytics;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Http;
using Qrs.Client.Api;

namespace Qrs.App.Analytics.Client
{
    public class AnalyticsClient : QrsApiClient
    {
        public const string APP_NAME = "QRS ANALYTICS";
        public AnalyticsClient(string baseUrl, ICookieProvider cookieProvider)
            :base(baseUrl, APP_NAME, cookieProvider)
        {
        }

        public async Task<ResponseModel<List<SessionLogItem>>> GetSessionLog()
        {
            // XyzGetSessionLog
            var response = await NpGetAsync<List<SessionLogItem>>("api/QrsTunnel/XyzGetSessionLog");
            if (response.IsError())
                return NpErrorResponse<List<SessionLogItem>>(response);

            return response;
        }

        public async Task<ResponseModel<string>> ClearSessionLog()
        {
            // XyzGetSessionLog
            var response = await NpPostAsync<string>("api/QrsTunnel/XyzClearSessionLog");
            if (response.IsError())
                return NpErrorResponse<string>(response);

            return response;
        }

    }
}
