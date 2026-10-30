using Google.Protobuf;
using Org.BouncyCastle.Crypto;
using Qrid.Common.Http;
using Qrid.Common.Proto;
using Qrid.Common.Proto.Crypto;
using Qrs.Proto;

namespace Qrs.Client.Context
{
    public class WrapperBase
    {
        public string AppName { get; set; }
        public QrsEnvelope QrsEnvelope { get; set; }

        public AssociatedData2 ADFromEnvelope;
        public AssociatedData2 ADFromData;
        public IdentityData IDPAY;

        public CryptoKey DEK;

        public ResponseModel Status;
        
        public bool IsModifiedPayload; // this is to be set externally, by UI code or so
        public bool IsModifiedAssociatedData; // this is detected automatically during Wrap etc operations

        public WrapperBase(string appName ) 
        {
            this.AppName = appName;
            Status = new ResponseModel();
        }

        #region virtual methods

        public virtual string GetPayloadType()
        {
            return null; // must be overwritten in a derived class
        }

        public virtual byte[] GetPayloadBytes()
        {
            return null; // must be overwritten in a derived class
        }

        public virtual TypedMessage GetPayloadMessage()
        {
            return TypedMessage.Create(GetPayloadType(), GetPayloadBytes());
        }

        public byte[] GetPayloadMessageBytes()
        {
            return GetPayloadMessage().ToByteArray();
        }

        public virtual string SetPayload(TypedMessage payloadMessage)
        {
            // must be overwritten in a derived class
            // must return error message or empty
            return null; 
        }

        public virtual bool GenerateNewDekOnModifiedPayload => true;

        public virtual ByteString GetImmutableDekId()
        {
            // may be overwritten in a derived class, if provided, must be immutable
            // allows derived wrapper class to provide Immutable DekId to be used in MA when creating new IdentityData
            // if not provided, MA would receive Empty, then MA will just use random 16-bytes DekId when constructing new IdentityData
            // if provided, then it will also be checked in IsValidIdentityData (so it must be immutable)
            return ByteString.Empty;
        }

        public virtual int GetImmutableTopLevelKey()
        {
            // may be overwritten in a derived class, if provided, must be immutable
            // allows derived wrapper class to provide TopLevelKey to be used in MA when creating new IdentityData
            return 0; 
        }

        public bool IsValidIdentityData(out string errorMessage)
        {
            // validation 0 - check DekId if provided by this wrapper
            var expectedDekId = GetImmutableDekId();
            if (!expectedDekId.IsEmpty && !IDPAY.DekId.Equals(expectedDekId))
            {
                errorMessage = $"Invalid QrsEnvelope.DekId='{IDPAY.DekId.ToBase64()}' which is different from expected immutable value provided by wrapper ='{expectedDekId.ToBase64()}'";
                return false;
            }

            if (IDPAY == null)
            {
                errorMessage = $"Invalid QrsEnvelope - Identity is missing";
                return false;
            }

            // validation 1 - check AppName
            if (IDPAY.AppName != AppName)
            {
                errorMessage = $"Invalid QrsEnvelope.AppName='{IDPAY.AppName}' which is different from expected value '{AppName}'";
                return false;
            }
            // validation 2 - check TopLevelKey
            if (IDPAY.TopLevelKey != GetImmutableTopLevelKey())
            {
                errorMessage = $"Invalid QrsEnvelope.TopLevelKey='{IDPAY.TopLevelKey}' which is different from this expected value of '{GetImmutableTopLevelKey()}'";
                return false;
            }
            // all good
            errorMessage = string.Empty;
            return true;
        }

        public virtual AssociatedData2 GetAssociatedData2()
        {
            // must be overwritten in a derived class
            return null; 
        }

        #endregion


    }
}
