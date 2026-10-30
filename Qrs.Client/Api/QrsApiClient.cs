using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.Client.Api
{
    public class QrsApiClient : CommonApiClient
    {
        public QrsApiClient()
        {
        }

        public QrsApiClient(string baseUrl, string appName, ICookieProvider cookieProvider) : base (baseUrl, appName, cookieProvider)
        {

        }

    }
}
