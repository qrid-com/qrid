using Qrs.TestConsole.IntegrationTests;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.HeadlessApps.CA
{
    public class ClientAppBase
    {
        public HeadlessUI HUI { get; set; }
        public Qrs.Client.Context.ClientContext ClientContext { get; set; }

        public ClientAppBase()
        {
        }

        public void LogDebug(string message)
        {
            Config.LogDebug(message);
        }


    }
}
