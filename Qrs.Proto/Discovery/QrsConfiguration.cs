using Qrid.Common.Proto.Const;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qrs.Proto.Discovery
{
    public class QrsConfiguration
    {
        public string TunnelServiceUri { get; set; }
        public string Domain { get; set; }

        // Optional API key required by the tunnel server to create new sessions. May be
        // supplied via domain discovery; a user-provided setting on the client takes precedence.
        public string ApiKey { get; set; }


        public QrsConfiguration()
        {
            
        }

    }
}
