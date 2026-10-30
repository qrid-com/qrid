using System;
using System.Collections.Concurrent;

namespace Qrs.Tunnel.Server.Models
{
    public class DirectionItem
    {
        public string Direction { get; set; }
        public ConcurrentBag<AwaitingTaskItem> AwaitingTasks { get; set; }
        public string Message { get; set; } // latest message sent in this direction
        public byte[] SenderPubKeyBytes { get; set; }
        public int MessageNumber { get; set; }
        public string MessageProtocolVersion { get; set; }
        
        public DateTime MessageDate { get; set; }
        
        public DirectionItem(string direction)
        {
            Direction = direction;
            AwaitingTasks = new ConcurrentBag<AwaitingTaskItem>();
        }

        public void CancelTasks()
        {
            while (AwaitingTasks.TryTake(out AwaitingTaskItem item))
            {
                item.PollingTaskSource.TrySetCanceled();
            }
        }
        
    }
}
