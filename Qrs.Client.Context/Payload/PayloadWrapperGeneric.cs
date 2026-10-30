//using Qrid.Common.Crypto;
//using Qrid.Common.Http;
//using Qrid.Common.Proto;
//using Qrs.Proto;
//using System.Security.Cryptography;
//using System;
//using System.Threading.Tasks;

//namespace Qrs.Client.Context.Payload
//{
//    public class PayloadWrapper<TPayload> : PayloadWrapper where TPayload : ProtoBase, new()
//    {
//        public TPayload Payload { get; set; }


//        public PayloadWrapper(ClientContext context) : base(context)
//        {
//        }

//        protected override byte[] GetPayloadBytes()
//        {
//            return Payload.GetByteArray();
//        }

//        public async Task<ResponseModel<QrsEnvelope>> WrapNewPayload()
//        {
//            var res = await WrapNewPayloadMessage(GetPayloadMessage());
//            if (res.IsError())
//                return ErrorResponse<QrsEnvelope>(res.Message);

//            QrsEnvelope = res.Data;
//            return res;
//        }

//        public async Task<ResponseModel<QrsEnvelope>> WrapUpdatedPayload()
//        {
//            var res = await WrapUpdatedPayloadMessage(GetPayloadMessage());
//            if (res.IsError())
//                return ErrorResponse<QrsEnvelope>(res.Message);

//            QrsEnvelope = res.Data;
//            return res;
//        }

//        public async Task<ResponseModel<TPayload>> UnwrapPayload(QrsEnvelope envelope)
//        {
//            var resp = await UnwrapPayloadMessage(envelope);
//            if (resp.IsError())
//                return ErrorResponse<TPayload>(resp.Message);

//            var res = new ResponseModel<TPayload>();
//            res.Data = ProtoBase.Create<TPayload>(resp.Data.MessageBytes);
//            return res;
//        }



//    }
//}
