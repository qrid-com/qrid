using Google.Protobuf;
using Qrid.Common.Proto.Crypto;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes : ProtoBase
    {

        public static HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        



    }
}