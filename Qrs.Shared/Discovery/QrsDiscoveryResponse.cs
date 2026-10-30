using Qrid.Common.Proto;
using Qrs.Proto.Discovery;

namespace Qrs.Shared.Discovery
{
    public class QrsDiscoveryResponse : BaseData
    {
        public QrsConfiguration QrsConfiguration { get; set; }
        public string Domain { get; set; }
    }
}
