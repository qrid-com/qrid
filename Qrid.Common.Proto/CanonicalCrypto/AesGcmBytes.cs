using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class AesGcmBytes : ProtoBase
    {

        public static AesGcmBytes Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AesGcmBytes Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AesGcmBytes Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static AesGcmBytes CreateWith(byte[] nonce, int macLength, byte[] ciphertextWithMac)
        {
            var res = new AesGcmBytes();
            res.Nonce = ByteString.CopyFrom(nonce);
            res.MacLength = macLength;
            res.BytesWithMac = ByteString.CopyFrom(ciphertextWithMac);
            return res;
        }



    }
}