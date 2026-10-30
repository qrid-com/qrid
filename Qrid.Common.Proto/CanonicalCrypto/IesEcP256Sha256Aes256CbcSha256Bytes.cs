using Google.Protobuf;
using Qrid.Common.Proto.Crypto;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class IesEcP256Sha256Aes256CbcSha256Bytes : ProtoBase
    {

        public static IesEcP256Sha256Aes256CbcSha256Bytes Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static IesEcP256Sha256Aes256CbcSha256Bytes Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static IesEcP256Sha256Aes256CbcSha256Bytes Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static IesEcP256Sha256Aes256CbcSha256Bytes CreateWith(CryptoKey pubKey, byte[] encryptedBytes, byte[] derivation, byte[] encoding)
        {
            var res = new IesEcP256Sha256Aes256CbcSha256Bytes();
            res.Derivation = ByteString.CopyFrom(derivation);
            res.Encoding = ByteString.CopyFrom(encoding);
            res.PublicKey = pubKey;
            res.Bytes = ByteString.CopyFrom(encryptedBytes);
            return res;
        }

        public static IesEcP256Sha256Aes256CbcSha256Bytes CreateWith(CanonicalKey pubKey, byte[] encryptedBytes, byte[] derivation, byte[] encoding)
        {
            var res = new IesEcP256Sha256Aes256CbcSha256Bytes();
            res.Derivation = ByteString.CopyFrom(derivation);
            res.Encoding = ByteString.CopyFrom(encoding);
            res.PublicKey = CryptoKey.FromCanonicalKey(pubKey);
            res.Bytes = ByteString.CopyFrom(encryptedBytes);
            return res;
        }



    }
}