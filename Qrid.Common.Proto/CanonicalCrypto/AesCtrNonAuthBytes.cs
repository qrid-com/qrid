using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class AesCtrNonAuthBytes : ProtoBase
    {

        public static AesCtrNonAuthBytes Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AesCtrNonAuthBytes Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AesCtrNonAuthBytes Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static AesCtrNonAuthBytes CreateWith(byte[] nonce, byte[] ciphertext)
        {
            var res = new AesCtrNonAuthBytes();
            res.Nonce = ByteString.CopyFrom(nonce);
            res.Bytes = ByteString.CopyFrom(ciphertext);
            return res;
        }



    }
}