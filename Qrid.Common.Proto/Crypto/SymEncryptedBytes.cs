using Google.Protobuf;
using Qrid.Common.Proto.CanonicalCrypto;
using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class SymEncryptedBytes : ProtoBase
    {

        public static SymEncryptedBytes Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static SymEncryptedBytes Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static SymEncryptedBytes Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public bool IsAssociatedData()
        {
            return IsAesGcm();
        }

        public bool IsAesGcm()
        {
            return (EncryptionAlgo == EncryptionAlgo.AesGcm256 || EncryptionAlgo == EncryptionAlgo.AesGcm128);
        }

        public AesGcmBytes GetAesGcmBytes()
        {
            if (!IsAesGcm())
                throw new InvalidOperationException($"Does not know how to deserialize EncryptionAlgo={EncryptionAlgo}");

            var res = AesGcmBytes.Create(Bytes);
            return res;
        }

    }
}