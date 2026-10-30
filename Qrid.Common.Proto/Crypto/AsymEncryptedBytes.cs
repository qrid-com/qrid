using Google.Protobuf;
using Qrid.Common.Proto.CanonicalCrypto;
using System;
using System.Text;

namespace Qrid.Common.Proto.Crypto
{
    public partial class AsymEncryptedBytes : ProtoBase
    {

        public static AsymEncryptedBytes Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AsymEncryptedBytes Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AsymEncryptedBytes Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static AsymEncryptedBytes CreateWith(IesEcP256Sha256Aes256CbcSha256Bytes encryptedBytes)
        {
            return new AsymEncryptedBytes()
            {
                EncryptionSchema = EncryptionSchema.IesEcP256Sha256Aes256CbcSha256,
                Bytes = encryptedBytes.GetByteString()
            };
        }

        public static AsymEncryptedBytes CreateWith(CanonicalKey pubKey, byte[] encryptedBytes, byte[] derivation, byte[] encoding)
        {
            return CreateWith(IesEcP256Sha256Aes256CbcSha256Bytes.CreateWith(pubKey, encryptedBytes, derivation, encoding));
        }

        public static AsymEncryptedBytes CreateWith(CryptoKey pubKey, byte[] encryptedBytes, byte[] derivation, byte[] encoding)
        {
            return CreateWith(IesEcP256Sha256Aes256CbcSha256Bytes.CreateWith(pubKey, encryptedBytes, derivation, encoding));
        }

        public static AsymEncryptedBytes CreateWith(HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes encryptedBytes)
        {
            return new AsymEncryptedBytes()
            {
                EncryptionSchema = EncryptionSchema.HybridEcP256Mlkem768Sha256Aes256CbcSha256,
                Bytes = encryptedBytes.GetByteString()
            };
        }

    }
}