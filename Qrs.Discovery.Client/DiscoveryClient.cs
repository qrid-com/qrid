using Qrid.Common.Client.Api;
using Qrid.Common.Http;
using Qrs.Proto.Discovery;
using Qrs.Shared.Discovery;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Qrs.Discovery.Client
{
    public class DiscoveryClient : CommonContext, IQrsDiscovery
    {
        private readonly ConcurrentDictionary<string, HttpResponseModel<QrsDiscoveryResponse>> cache;

        // must be thread-safe b/c we use singleton here
        public static DiscoveryClient Instance = new DiscoveryClient();

        public DiscoveryClient()
        {
            cache = new ConcurrentDictionary<string, HttpResponseModel<QrsDiscoveryResponse>>();
        }

        public async Task<HttpResponseModel<StringResponse>> GetServiceUriAsync(string domain, string serviceName)
        {
            
            var conf = await GetDomainQrsConfigurationAsync(domain);
            if (!conf.IsSuccess())
                return ErrorResponse<StringResponse>(conf);

            // for now, ignore servicename, always return TunnelServiceUri
            var responseData = GetStringResponse(conf.Data.QrsConfiguration?.TunnelServiceUri);
            return SuccessResponse<StringResponse>(responseData);

        }

        public async Task<HttpResponseModel<QrsDiscoveryResponse>> GetDomainQrsConfigurationAsync(string domain)
        {
            // 1. Check Cache
            if (cache.ContainsKey(domain))
                return cache[domain];

            // 2. if not in Cache, fetch from remote server
            var res = await DiscoverDomainConfigurationAsync(domain);
            cache[domain] = res;

            return res;
        }

        public async Task<HttpResponseModel<QrsDiscoveryResponse>> DiscoverDomainConfigurationAsync(string domain)
        {
            // fetch from remote server

            // 1) try fetch from "qrs.domain.com"
            var response1 = await FetchDomainConfigurationAsync(domain, $"qrs.{domain}");
            if (response1.IsSuccess())
                return response1;

            // 2) if not, then try fetch from "domain.com"
            var response2 = await FetchDomainConfigurationAsync(domain, domain);
            if (response2.IsSuccess())
                return response2;

            return ErrorResponse<QrsDiscoveryResponse>($"Cannot discover QRS configuration for domain='{domain}'. Please see _here_ on how to configure your domain to support QRS protocol.");
        }

        public async Task<HttpResponseModel<QrsDiscoveryResponse>> FetchDomainConfigurationAsync(string domain, string host)
        {
            // creating HttpClient here so we could be thread-safe with that static .Instance variable
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(3); // 5 sec - sometimes just resolving DNS takes 5 sec

            var res = SuccessResponse<QrsDiscoveryResponse>();
            res.Data.Domain = domain;
            var url = $"http://{host}/.well-known/qrs-configuration"; // TODO - change to HTTPS
            LogDebug($"GET {url}");
            
            await LogDnsInfoAsync(host);
            
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                return ErrorResponse<QrsDiscoveryResponse>(e);
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            res.Data.QrsConfiguration = new QrsConfiguration();
            res.Data.QrsConfiguration.TunnelServiceUri = responseBody;

            return res;
        }

        public async Task LogDnsInfoAsync(string host)
        {
            // do NOT do this in WebAssembly b/c browser does nto allow to do DNS 
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("WEBASSEMBLY")))
                return;

            try
            {
                var ipArray = await Dns.GetHostAddressesAsync(host);
                string ipAddress = ipArray.Length > 0 ? ipArray[0].ToString() : null;
                LogDebug($"Domain Discovery - DNS info - HOST:{host}, IPADDRESS:{ipAddress}");
            }
            catch (Exception ex)
            {
                LogDebug($"Domain Discovery - DNS info - HOST:{host} - DNS ERROR: {ex.Message}");
            }
        }

    }
}
