namespace Qrid.Common.Client.Cookie
{
    public interface ICookieProvider
    {
        string GetCookie(string host);
        void SetCookie(string host, string cookie);


    }
}
