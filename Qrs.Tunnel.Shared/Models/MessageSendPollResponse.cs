namespace Qrs.Tunnel.Shared.Models
{
    public class MessageSendPollResponse : Qrid.Common.Proto.BaseData
    {
        public string Message { get; set; }
        public byte[] SenderPubKeyBytes { get; set; }
        public int MessageNumber { get; set; }
        public string MessageProtocolVersion { get; set; } // QRS Tunnel Client Protocol version of the Message sender (SemVer) - bound to Message via AEAD
        public string Status { get; set; }
        public string ServerProtocolVersion { get; set; } // QRS Tunnel Server Protocol version (SemVer), see QrsTunnelProtocol

        public bool IsTimeout()
        {
            return Status == "TIMEOUT";
        }

        public MessageSendPollResponse()
        {
        }
    }
}
