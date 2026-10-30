using System.Collections.Generic;

namespace Qrs.App.Vault.Client.Models
{
    public class Settings
    {
        public string TunnelServerDomain { get; set; } 
        public string TunnelServerUri { get; set; }

        // Optional API key required by some Tunnel Servers to create new sessions
        // (see Qrs.Tunnel.Server QrsTunnel:ApiKey). Null/empty means no api key is sent.
        public string ApiKey { get; set; }
        public bool IsConfirmed { get; set; }

        // most recently used vault files, most recent first
        public List<string> RecentFiles { get; set; } = new List<string>();

        public Settings()
        {
            
        }
    }
    
}
