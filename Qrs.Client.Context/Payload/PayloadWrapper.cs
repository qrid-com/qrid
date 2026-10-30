//using Qrid.Common.Http;
//using Qrs.Proto;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading.Tasks;
//using Qrs.Proto.Extensions;
//using Qrid.Common.Crypto;
//using System.Net.Sockets;
//using Qrid.Common.Proto;
//using Qrid.Common.Proto.Crypto;
//using Qrid.Common.Client.Api;

//namespace Qrs.Client.Context.Payload
//{
//    public class PayloadWrapper
//    {
//        public ClientContext Context { get; set; }
//        public QrsEnvelope QrsEnvelope { get; set; }

//        public AssociatedDataDek ADDEKFromSEB;
//        public AssociatedDataDek ADDEKFromData;
//        public AssociatedDataPayload ADPAY;
        

//        private CryptoKey DEK;

//        public PayloadWrapper(ClientContext context) 
//        {
//            this.Context = context;
//        }

//        #region virtual methods

//        public virtual string GetPayloadType()
//        {
//            return null; // must be overwritten in a derived class
//        }

//        protected virtual byte[] GetPayloadBytes()
//        {
//            return null; // must be overwritten in a derived class
//        }

//        protected virtual TypedMessage GetPayloadMessage()
//        {
//            return TypedMessage.Create(GetPayloadType(), GetPayloadBytes());
//        }

//        protected virtual AssociatedDataDek GetAssociatedDataDek()
//        {
//            return null; // must be overwritten in a derived class
//        }

//        #endregion

//        public async Task<ResponseModel<QrsEnvelope>> WrapNewPayloadMessage(
//            Qrid.Common.Proto.TypedMessage payload
//        )
//        {
//            var res = new ResponseModel<QrsEnvelope>();

//            // ADDEK from DATA
//            ADDEKFromData = GetAssociatedDataDek();
//            ADDEKFromData.AppName = Context.AppName;

//            // DEK - generate new key
//            DEK = CryptoUtils.GetRandomSymKey();

//            // if DekId is not provided, generate new DekId 
//            if (string.IsNullOrWhiteSpace(ADDEKFromData.DekId))
//                ADDEKFromData.DekId = CryptoUtils.GetRandomBytesToString(16);

//            // WRAP DEK + ADDEK
//            var request = Context.GetWrapKeyRequest(ADDEKFromData.DekId, DEK, ADDEKFromData);
//            var response = await Context.WrapKeyAsync(request);
//            if (response.IsError())
//                return ErrorResponse<QrsEnvelope>(response.Message);

//            // ADPAY
//            ADPAY = new AssociatedDataPayload();
//            ADPAY.DekId = ADDEKFromData.DekId;
//            ADPAY.AppName = ADDEKFromData.AppName;

//            // Wrap Payload into QrsEnvelope
//            QrsEnvelope = new QrsEnvelope();
//            QrsEnvelope.EncryptedDek = response.Data.Items[0].WrappedKey;
//            QrsEnvelope.EncryptedPayload = CryptoUtils.SymEncrypt(payload, DEK, AssociatedData.Create(ADPAY));

//            // Success
//            res.Data = QrsEnvelope;
//            return res;
//        }

//        public async Task<ResponseModel<QrsEnvelope>> WrapUpdatedPayloadMessage(
//            Qrid.Common.Proto.TypedMessage payload
//        )
//        {
//            // assuming original payload was UNWRAPPED from .QrsEnvelope
//            // so original ADDEK/ADPAY could be obtained from .QrsEnvelope SEB's
//            // and .DEK is available

//            var res = new ResponseModel<QrsEnvelope>();
            
//            // ADDEK from SEB
//            ADDEKFromSEB = QrsEnvelope.EncryptedDek.GetAssociatedDataDek();

//            // ADDEK from DATA
//            ADDEKFromData = GetAssociatedDataDek();
//            ADDEKFromData.CopyCarriedOverFields(ADDEKFromSEB); // Kek, TLK, Created
//            ADDEKFromData.AppName = Context.AppName;
//            ADDEKFromData.DekId = ADDEKFromSEB.DekId;

//            // has AD changed b/c of payload (and other data) being updated?
//            var diff = ADDEKFromSEB.DifferenceWith(ADDEKFromData);
//            if (diff != string.Empty)
//            {
//                // AD has changed, need to perform RESEALING operation using QRS mobile app
//                // "RESEALING" means only AD has changed, DEK remains the same
                 
//                // RESEAL using QRS app
//                var request = Context.GetResealKeyRequest(ADDEKFromData.DekId, QrsEnvelope.EncryptedDek, ADDEKFromSEB, ADDEKFromData, true);
//                var response = await Context.ResealKeyAsync(request);
//                if (response.IsError())
//                    return ErrorResponse<QrsEnvelope>(response.Message);

//                // save resealed DEK into EXISTING QrsEnvelope instance
//                QrsEnvelope.EncryptedDek = response.Data.Items[0].WrappedKey;
//            }

//            // Payload
               
//            // ADPAY
//            ADPAY = new AssociatedDataPayload();
//            ADPAY.DekId = ADDEKFromData.DekId;
//            ADPAY.AppName = ADDEKFromData.AppName;

//            // Wrap Payload into EXISTING QrsEnvelope instance
//            QrsEnvelope.EncryptedPayload = CryptoUtils.SymEncrypt(payload, DEK, AssociatedData.Create(ADPAY));

//            // Success
//            res.Data = QrsEnvelope;
//            return res;
//        }

//        public async Task<ResponseModel<TypedMessage>> UnwrapPayloadMessage(
//            QrsEnvelope envelope
//        )
//        {
//            var res = new ResponseModel<TypedMessage>();

//            // ADDEK from DATA
//            ADDEKFromData = GetAssociatedDataDek();
//            // ADDEK from SEB
//            ADDEKFromSEB = envelope.EncryptedDek.GetAssociatedDataDek();
//            // ADPAY
//            ADPAY = envelope.EncryptedPayload.GetAssociatedDataPayload();

//            // validation 1 - check DekId and AppName in SEB
//            var diff1 = ADDEKFromSEB.DifferenceWithAdPay(ADPAY, Context.AppName);
//            if (diff1 != string.Empty)
//                return ErrorResponse<TypedMessage>($"Invalid QrsEnvelope (SEB): {diff1}");

//            // validation 2 - check AD Items in SEB and DATA
//            var diff2 = ADDEKFromSEB.DifferenceInItemsWith(ADDEKFromData);
//            if (diff2 != string.Empty)
//                return ErrorResponse<TypedMessage>($"Invalid QrsEnvelope (ADI): {diff2}");

//            // unwrap DEK (use QRS app)
//            var response = await Context.UnwrapKeyAsync(envelope.EncryptedDek);
//            if (response.IsError())
//                return ErrorResponse<TypedMessage>(response.Message);
            
//            // we've got DEK key unwrapped!
//            DEK = response.Data.Items[0].UnwrappedKey;
//            QrsEnvelope = envelope;

//            // Decrypt Payload
//            var payloadBytes = CryptoUtils.SymDecrypt(envelope.EncryptedPayload, DEK, envelope.IdBytes); // TODO - add try/catch ?
//            res.Data = TypedMessage.Create(payloadBytes);

//            // Validate Payload type
//            if (res.Data.MessageType != GetPayloadType())
//                return ErrorResponse<TypedMessage>($"Unexpected PayloadType - got '{res.Data.MessageType}' while expecting '{GetPayloadType()}'");

//            return res;
//        }

//        protected ResponseModel<T> ErrorResponse<T>(string message)
//        {
//            return CommonContext.NpErrorResponse<T>(message);
//        }

//    }
//}
