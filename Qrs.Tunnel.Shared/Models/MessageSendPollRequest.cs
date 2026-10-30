namespace Qrs.Tunnel.Shared.Models
{
    public class MessageSendPollRequest
    {
        public string TunnelHashId { get; set; } // PubKey Hash that also identifies the Tunnel, usually it belongs to CA
        public byte[] SenderPubKeyBytes { get; set; } // full sender public key, Hash from QR code will be used to verify SenderPubKeyBytes before using as SenderPubKey
        public string AppName { get; set; } 
        public string Message { get; set; }
        public int MessageNumber { get; set; }
        public string MessageProtocolVersion { get; set; } // sender's QRS Tunnel Client Protocol version (SemVer) - bound to Message via AEAD, so Tunnel Server cannot modify it
        public string Direction { get; set; } // "MA2CA" or "CA2MA" (Mobile Application or Client Application) - 
        public int PollingTimeout { get; set; } // 0 means no polling, return immediately
        public int ExpectedResponseNumber { get; set; } // Response.MessageNumber must be >= ExpectedResponseNumber
        public bool ExpectResponse { get; set; }

        //public bool IsSenderMa() => Direction == "MA2CA";
        //public bool IsSenderCa() => !IsSenderMa();

        public bool HasMessageToSend()
        {
            return Message != null;
        }

        public MessageSendPollRequest()
        {
        }
    }
}
