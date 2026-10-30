using System.IO;

namespace Qrid.Common.Client.Cookie
{
    public class FileCookieProvider : ICookieProvider
    {
        private string _cookieFolder = null;
        private MemoryCookieProvider _cache = null;  

        public FileCookieProvider(string cookieFolder)
        {
            _cookieFolder = cookieFolder;
            _cache = new MemoryCookieProvider();
        }

        private string GetHost(string host)
        {
            return _cache.GetHost(host);
        }

        private string GetCookieFile(string host)
        {
            return Path.Combine(_cookieFolder, $"_cookie_instance_idt_{GetHost(host)}");
        }

        public string GetCookie(string host)
        {
            var cookie = _cache.GetCookie(host);
            if (cookie == null)
            {
                // not in cache, try loading from file
                var file = GetCookieFile(host);
                if (File.Exists(file))
                {
                    // read from file and also load into cache
                    cookie = File.ReadAllText(file) ?? string.Empty;
                    _cache.SetCookie(host, cookie);
                }
            }
            return cookie;
        }

        public void SetCookie(string host, string cookie)
        {
            var existingCookie = _cache.GetCookie(host);
            if ( (existingCookie ?? string.Empty) != ( cookie ?? string.Empty) )
            {
                // different value, updating the cookie
                _cache.SetCookie(host, cookie);
                var file = GetCookieFile(host);
                File.WriteAllText(file, cookie);
            }
        }
    }
}
