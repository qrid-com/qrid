using Qrid.Common.Client.Cookie;
using Microsoft.Maui.Storage;

namespace Qrs.Mobile.App.MobileDevice
{
    public class PrefCookieProvider : ICookieProvider
    {
        private MemoryCookieProvider _cache = null;

        public PrefCookieProvider()
        {
            _cache = new MemoryCookieProvider();
        }

        private string GetHost(string host)
        {
            return _cache.GetHost(host);
        }

        private string GetCookieKeyName(string host)
        {
            return $"_cookie_instance_idt_{GetHost(host)}";
        }

        public string GetCookie(string host)
        {
            var cookie = _cache.GetCookie(host);
            if (cookie == null)
            {
                // not in cache, try loading from Preferences
                var keyName = GetCookieKeyName(host);
                if (Preferences.ContainsKey(keyName))
                {
                    // read from Preferences and also load into cache
                    cookie = Preferences.Get(keyName, string.Empty) ?? string.Empty;
                    _cache.SetCookie(host, cookie);
                }
            }
            return cookie;
        }

        public void SetCookie(string host, string cookie)
        {
            var existingCookie = _cache.GetCookie(host);
            if ((existingCookie ?? string.Empty) != (cookie ?? string.Empty))
            {
                // different value, updating the cookie
                _cache.SetCookie(host, cookie);
                var keyName = GetCookieKeyName(host);
                Preferences.Set(keyName, cookie);
            }
        }

    }
}
