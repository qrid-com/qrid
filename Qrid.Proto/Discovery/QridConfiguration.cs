using Qrid.Common.Proto.Const;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Qrid.Proto.Discovery
{
    public class QridConfiguration
    {
        public string Domain { get; set; } // domain to which this config is related to, for example "company.com" if qrid usernames are like "alex@company.com"
        public List<ServiceItem> Services { get; set; } // base uri of all API's  

        // Reputation Service domain(s) used by this domain's messaging servers.
        // Empty => this domain accepts new contacts WITHOUT contacting a Rep Server (ANSWERS 1 #1).
        // A single domain for now (e.g. "qrid.com"); comma-separated list reserved for the future.
        public string RepServiceDomains { get; set; }

        [JsonIgnore]
        public string TunnelServiceUri => GetServiceUri(ServiceType.TunnelApi);
        [JsonIgnore]
        public string KeyServiceUri => GetServiceUri(ServiceType.KeyApi);
        [JsonIgnore] 
        public string DirServiceUri => GetServiceUri(ServiceType.DirApi);
        [JsonIgnore] 
        public string MessagingServiceUri => GetServiceUri(ServiceType.MessagingApi);
        [JsonIgnore] 
        public string MobileServiceUri => GetServiceUri(ServiceType.MobileApi);
        [JsonIgnore] 
        public string StorageServiceUri => GetServiceUri(ServiceType.StorageApi);
        [JsonIgnore]
        public string AdminServiceUri => GetServiceUri(ServiceType.AdminApi);

        public QridConfiguration()
        {
            Services = new List<ServiceItem>();
        }

        public string GetServiceUri(string serviceName)
        {
            return Services.FirstOrDefault(s => s.ServiceName == serviceName)?.BaseUri;
        }

        public void SetServiceUri(string serviceName, string serviceUri)
        {
            var item = Services.FirstOrDefault(s => s.ServiceName == serviceName);
            if (item != null)
            {
                item.BaseUri = serviceUri;
            }
            else
            {
                Services.Add(new ServiceItem { BaseUri = serviceUri, ServiceName = serviceName });
            }
        }
    }
}
