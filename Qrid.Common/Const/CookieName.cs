namespace Qrid.Common.Const
{
    public class CookieName
    {
        public const string InstanceIdt = "instance_idt";

        // Optional API key that a QRS Client Application sends as a cookie so the
        // Qrs.Tunnel.Server can authorize creation of new tunnel sessions. See
        // Qrs.Tunnel.Server (QrsTunnel:ApiKey) for the server-side check.
        public const string QrsTunnelApiKey = "qrs_tunnel_api_key";
    }
}
