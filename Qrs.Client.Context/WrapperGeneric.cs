using Qrid.Common.Proto;

namespace Qrs.Client.Context
{
    public class Wrapper<TPayload> : WrapperBase where TPayload : ProtoBase, new()
    {
        public TPayload Payload { get; set; }

        public Wrapper(string appName) : base(appName)
        {
            //Payload = new TPayload(); otherwise Secret comes out as "Open" IsSecretOpen
        }

        public void CreatePayload()
        {
            Payload = new TPayload();
        }

        public void ClearPayload()
        {
            Payload = null;
            DEK = null;
            // TODO - do we also need to clear ADxxx objects?
        }

        public override byte[] GetPayloadBytes()
        {
            return Payload.GetByteArray();
        }

        public override string SetPayload(TypedMessage payloadMessage)
        {
            if (payloadMessage.MessageType == GetPayloadType())
            {
                Payload = ProtoBase.Create<TPayload>(payloadMessage.MessageBytes);
                return string.Empty;
            }
            else
            {
                return $"Unexpected MessageType in payload's TypedMessage - got '{payloadMessage.MessageType}' while expecting '{GetPayloadType()}'";
            }
        }


    }
}
