using System;

namespace Qrs.Tunnel.Server.Models
{
    public class MessageItem
    {
        public int MessageNumber;
        public string MessageType { get; set; }
        public string Message { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
