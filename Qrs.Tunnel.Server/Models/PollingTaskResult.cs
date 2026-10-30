namespace Qrs.Tunnel.Server.Models
{
    public class PollingTaskResult
    {
        public string Message { get; set; }
        public byte[] SenderPubKeyBytes { get; set; }
        public int MessageNumber { get; set; }
        public string MessageProtocolVersion { get; set; }
        public string Error { get; set; }

        public bool IsError => !string.IsNullOrEmpty(Error);

    }
}
