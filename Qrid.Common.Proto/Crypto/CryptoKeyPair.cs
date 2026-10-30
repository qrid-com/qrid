using Google.Protobuf;
using Qrid.Common.Proto.CanonicalCrypto;
using System;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CryptoKeyPair : ProtoBase
    {
        public static CryptoKeyPair Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoKeyPair Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoKeyPair Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static CryptoKeyPair Create(CC.CanonicalKey pubKey, CC.CanonicalKey privKey)
        {
            var res = new CryptoKeyPair();
            res.PrivateKey = CR.CryptoKey.FromCanonicalKey(privKey);
            res.PublicKey = CR.CryptoKey.FromCanonicalKey(pubKey);
            return res;
        }

        public static CryptoKeyPair Create(CC.EccKeyPair keyPair)
        {
            return Create(keyPair.PublicKey, keyPair.PrivateKey);
        }

        public static CryptoKeyPair CreateWith(CryptoKeyPair eccPair, CryptoKeyPair mlkPair)
        {
            var hybridPair = new CR.CryptoKeyPair();
            hybridPair.PrivateKey = CR.CryptoKey.CreateWith(HybridEcMlKemPrivateKey.CreateWith(eccPair.PrivateKey, mlkPair.PrivateKey), KeyAlgo.HybridEcMlKem, KeyType.Private);
            hybridPair.PublicKey = CR.CryptoKey.CreateWith(HybridEcMlKemPublicKey.CreateWith(eccPair.PublicKey, mlkPair.PublicKey), KeyAlgo.HybridEcMlKem, KeyType.Public);
            return hybridPair;
        }


        public CC.EccKeyPair ToEccKeyPair()
        {
            var res = new CC.EccKeyPair();
            res.PublicKey = this.PublicKey?.ToEccPublicKey();
            res.PrivateKey = this.PrivateKey?.ToEccPrivateKey();
            return res;
        }


        public override DataType GetDataType()
        {
            return DataType.CryptoKeyPair;
        }

    }
}