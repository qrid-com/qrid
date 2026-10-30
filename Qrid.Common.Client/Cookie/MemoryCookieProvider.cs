using System.Collections.Generic;

namespace Qrid.Common.Client.Cookie
{
    public class MemoryCookieProvider : ICookieProvider
    {
        private Dictionary<string,string> _cookies = new Dictionary<string, string>();

        public string GetHost(string host)
        {
            return host?.ToLower()?.Trim() ?? string.Empty;
        }

        public string GetCookie(string host)
        {
            var hostLower = GetHost(host);
            return _cookies.ContainsKey(hostLower) ? _cookies[hostLower] : null; // null is important here, it signals that host is not in Dictionary
        }

        public void SetCookie(string host, string cookie)
        {
            var hostLower = GetHost(host);
            _cookies[GetHost(hostLower)] = cookie ?? string.Empty; // never storing null is important here
        }
    }
}
