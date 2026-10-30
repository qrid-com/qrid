using Qrid.Common.Proto;
using Qrid.Common.Proto.Crypto;
using Qrs.Proto.Api;
using QCP = Qrid.Common.Proto;

namespace Qrs.Tunnel.Client
{
    public class MsgHelper: QCP.ProtoBase
    {
        public string ErrorMessage;
        public QCP.TypedMessage TypedMessageSym; // sym encrypted - outer layer, closer to surface
        public QCP.TypedMessage TypedMessageAsym; // asym encrypted - inner layer, close to original plain text data

        public AsymEncryptedBytesWithKeyId AsymEncrypted;

        public QCP.TypedMessage TypedMessage => TypedMessageAsym ?? TypedMessageSym;

        public MsgHelper SetError(string errorMessage)
        {
            ErrorMessage = errorMessage;
            return this;
        }

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
        public bool IsDoubleEncrypted => TypedMessageSym?.MessageType == TunnelMessageType.ASYM_ENCRYPTED;
        public bool IsKeyIdDifferent => (AsymEncrypted != null) && (TypedMessageAsym == null);
        public bool IsPayload => TypedMessageAsym?.MessageType == TunnelMessageType.PAYLOAD;
        public bool IsSwitchRequested => TypedMessage?.MessageType == TunnelMessageType.SWITCH_PUBKEY_REQUESTED;
        public bool IsSwitchRejected => TypedMessage?.MessageType == TunnelMessageType.SWITCH_PUBKEY_REJECTED;
        public bool IsPairingRequestedByMa => TypedMessageAsym?.MessageType == TunnelMessageType.PAIRING_REQUESTED_MA;
        public bool IsPairingRequestedByCa => TypedMessage?.MessageType == TunnelMessageType.PAIRING_REQUESTED_CA;

        public PairingRequest GetPairingRequest()
        {
            return PairingRequest.Create(TypedMessageAsym.MessageBytes);
        }

        public string GetSwitchPubKeyRequest()
        {
            return StringObject.Create(TypedMessageAsym.MessageBytes).Value;
        }

    }
}
