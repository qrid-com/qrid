using Qrid.Common.Client.Cookie;
using Qrid.Common.Client.UI;
using Qrid.Common.Http;
using Qrs.App.SudoAskPass.Cli.Terminal;
using Qrs.Client.Context;
using Qrs.Tunnel.Client;

namespace Qrs.App.SudoAskPass.Cli.Context
{
    public partial class SudoAskPassContext : ClientContext
    {
        public const string APP_NAME = "SUDO ASKPASS";

        public string TunnelServiceUri { get; set; }
        public string TunnelServiceDomain { get; set; }

        public TerminalIo Terminal { get; set; }

        public SudoAskPassContext(
            IUserInteraction ui,
            TerminalIo terminal,
            string tunnelServiceDomain,
            string tunnelServiceUri,
            IAsymKeyItemProvider asymKeyItemProvider,
            ICookieProvider cookieProvider,
            string? apiKey = null) :
            base(APP_NAME, ui, asymKeyItemProvider, cookieProvider)
        {
            Terminal = terminal;
            TunnelServiceDomain = tunnelServiceDomain;
            TunnelServiceUri = tunnelServiceUri;
            ApiKey = apiKey;
        }

        public async Task<HttpResponseModel<StringResponse>> InitFromConfigAsync()
        {
            var initResponse = await InitFromConfigAsync(TunnelServiceDomain, TunnelServiceUri, ApiKey);
            WriteInfo("Tunnel Server: " + (TunnelClient?.BaseUrl ?? "not initialized"));
            return initResponse;
        }

        // All diagnostic output goes to the terminal/stderr, never to stdout
        // (stdout is reserved for the decrypted sudo password).

        public void WriteInfo(string message)
        {
            Terminal.WriteLine(message);
        }

        public void WriteError(string message)
        {
            Terminal.WriteLine($"ERROR - {message}");
        }
    }
}
