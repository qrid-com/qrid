using Qrs.Mobile.Client.Context;
using Qrs.TestConsole.HeadlessApps.CA;
using Qrs.TestConsole.HeadlessApps.MA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.IntegrationTests
{
    public class HeadlessUser
    {
        private string _username;
        private string _userRootPath;
        public MobileApp MA { get; set; }
        public ClientApps CA { get; set; }
        public string Username => _username;
        public string FullUserName => $"{_username}@{Config.Domain}";

        public HeadlessUser(string username)
        {
            _username = username;
            _userRootPath = System.IO.Path.Combine(Config.RootDir, username);

            MA = new MobileApp(username);
            CA = new ClientApps(username, _userRootPath);
        }

        public async Task ReOpenMA(MobileApp oldMA)
        {
            // simulates that user "closes MA" then "re-launches" MA
            // we should use this most of the time instead of MA.Logout() <<< this among other things, wipes out paired PubKeys, and is a rare flow anyways
            // much more common and used flow is "close" then "open"

            MA = new MobileApp(_username);
            await MA.InitAsync();

            if (oldMA != null)
            {
                // carry-over backup data
                MA.CopyBackupData(oldMA);
            }

        }

        public async Task ReOpenVaultCA()
        {
            // simulates that user "closes CA" then "re-launches" CA
            
            CA.VaultApp = new VaultApp(_username, _userRootPath);
            await CA.VaultApp.InitAsync();

        }

        public async Task ReOpenPwmCA()
        {
            // simulates that user "closes CA" then "re-launches" CA

            CA.PwmApp = new PwmApp(_username, _userRootPath);
            await CA.PwmApp.InitAsync();

        }

    }
}
