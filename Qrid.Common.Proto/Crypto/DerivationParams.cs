using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class DerivationParams : ProtoBase
    {

        public static DerivationParams Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static DerivationParams Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static DerivationParams Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        


    }
}
