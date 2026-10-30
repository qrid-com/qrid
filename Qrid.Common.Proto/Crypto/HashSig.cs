using Google.Protobuf;
using Qrid.Common.Proto.CanonicalCrypto;
using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class HashSig : ProtoBase
    {

        public static HashSig Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HashSig Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HashSig Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static HashSig CreateWith(CryptoHash hash, CryptoSig sig)
        {
            var res = new HashSig();
            res.CryptoHashBytes = hash.ToByteString();
            res.Sig = sig;
            return res;
        }

        public string Explain()
        {
            return $"Hash:{CryptoHash.Create(CryptoHashBytes).Explain()}, Sig:{Sig?.Explain()}'";
        }


    }
}