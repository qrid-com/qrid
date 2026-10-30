using System;

namespace Qrs.Tunnel.Server.Models
{
    public class Stats
    {
        public long TimerTicks { get; set; }
        public int ActiveSessionCount { get; set; }
        public int TotalSessionCount { get; set; }
        public int DequeueErrorsCount  { get; set; }
        public int RemoveErrorsCount { get; set; }
        public DateTime Created { get; set; } = DateTime.Now;
    }
}
