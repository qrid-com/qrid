using Qrid.Common.Http;
using System.Threading.Tasks;

namespace Qrs.Shared.Discovery
{
    public interface IQrsDiscovery
    {
        Task<HttpResponseModel<QrsDiscoveryResponse>> GetDomainQrsConfigurationAsync(string domain);
        Task<HttpResponseModel<StringResponse>> GetServiceUriAsync(string domain, string serviceName);
    }
}