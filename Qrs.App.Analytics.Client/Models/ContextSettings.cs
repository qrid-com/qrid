using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.App.Analytics.Client.Models
{
    public class ContextSettings
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public string BaseUri { get; set; }
        public string SQLiteFile { get; set; }
        // Admin api key of the QRS Tunnel Server; required by the XyzGetSessionLog / XyzClearSessionLog endpoints.
        public string ApiKey { get; set; }
        
        public ContextSettings()
        {

        }
    }
}
