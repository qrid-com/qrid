using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.HeadlessApps.CA
{
    public class ClientApps
    {
        //public AdminApp AdminApp { get; set; }
        public VaultApp VaultApp { get; set; }
        public PwmApp PwmApp { get; set; }

        public ClientApps(string username, string userRootPath)
        {
            VaultApp = new VaultApp(username, userRootPath);
            PwmApp = new PwmApp(username, userRootPath);
        }
    }
}
