using Microsoft.AspNetCore.Components;
using Qrs.App.PasswordManager.Client.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.PasswordManager.Components.Code
{
    public class Settings
    {
        public string TunnelServerDomain { get; set; } 
        public string TunnelServerUri { get; set; }

        // Optional API key required by some Tunnel Servers to create new sessions
        // (see Qrs.Tunnel.Server QrsTunnel:ApiKey). Null/empty means no api key is sent.
        public string ApiKey { get; set; }
        public string DataPath { get; set; }

        public string BackupFolders { get; set; } // ";" separated list of folders to backup to that UI shows as backup options on Backup screen
        public string SelectedBackupFolders { get; set; } // ";" separated list of folders actually selected during the last backup
        public bool IsConfirmed { get; set; }
        public Settings()
        {
            
        }
    }
    
}
