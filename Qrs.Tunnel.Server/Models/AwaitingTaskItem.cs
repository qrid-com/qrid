using System;
using System.Threading.Tasks;

namespace Qrs.Tunnel.Server.Models
{
    public class AwaitingTaskItem
    {
        public TaskCompletionSource<PollingTaskResult> PollingTaskSource { get; set; }
        public string PubKey { get; set; }
        public string EncryptedData { get; set; }
        public string Signature { get; set; }
        public DateTime CreateDate { get; set; }

        public AwaitingTaskItem()
        {
            CreateDate = DateTime.Now;
        }
    }
}