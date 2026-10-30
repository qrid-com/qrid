using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class KdfParams : ProtoBase
    {

        public static KdfParams Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static KdfParams Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static KdfParams Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static KdfParams Create(KdfType kdfType, ProtoBase kdfParams)
        {
            var res = new KdfParams()
            {
                KdfType = kdfType,
                KdfParams_ = kdfParams?.GetByteString()
            };
            return res;
        }


    }
}
