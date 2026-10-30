using Google.Protobuf;
using Qrid.Common.Proto.CanonicalCrypto;
using System;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using GPB = Google.Protobuf;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CryptoKey : ProtoBase
    {

        public static CryptoKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CryptoKey Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static CryptoKey CreateWith(byte[] keyBytes, KeyAlgo keyAlgo, KeyType keyType)
        {
            var res = new CryptoKey();
            res.KeyAlgo = keyAlgo;
            res.KeyType = keyType;
            res.KeyBytes = ByteString.CopyFrom(keyBytes);
            return res;
        }

        public static CryptoKey CreateWith(ProtoBase key, KeyAlgo keyAlgo, KeyType keyType)
        {
            var res = new CryptoKey();
            res.KeyAlgo = keyAlgo;
            res.KeyType = keyType;
            res.KeyBytes = key.GetByteString();
            return res;
        }

        public static CryptoKey FromCanonicalKey(CC.CanonicalKey canonicalKey)
        {
            var res = new CryptoKey();
            res.KeyAlgo = canonicalKey.KeyAlgo;
            res.KeyType = canonicalKey.KeyType;
            res.KeyBytes = canonicalKey.GetByteString();
            return res;
        }

        public EccPublicKey ToEccPublicKey()
        {
            // TODO - refactor this ugly sh!t
            var res = EccPublicKey.Create(this.KeyBytes);
            if (res.KeyAlgo != KeyAlgo) throw new ApplicationException($"Error in ToEccPublicKey() - res.KeyAlgo ({res.KeyAlgo}) != KeyAlgo ({KeyAlgo})");
            if (res.KeyType != KeyType) throw new ApplicationException($"Error in ToEccPublicKey() - res.KeyType ({res.KeyType}) != KeyType ({KeyType})");
            return res;
        }

        public EccPrivateKey ToEccPrivateKey()
        {
            // TODO - refactor this ugly sh!t
            var res = EccPrivateKey.Create(this.KeyBytes);
            if (res.KeyAlgo != KeyAlgo) throw new ApplicationException($"Error in ToEccPublicKey() - res.KeyAlgo ({res.KeyAlgo}) != KeyAlgo ({KeyAlgo})");
            if (res.KeyType != KeyType) throw new ApplicationException($"Error in ToEccPublicKey() - res.KeyType ({res.KeyType}) != KeyType ({KeyType})");
            return res;
        }

        public HybridEcMlKemPublicKey ToHybridEcMlKemPublicKey()
        {
            if (KeyAlgo != KeyAlgo.HybridEcMlKem) throw new ApplicationException($"Incompatible KeyAlgo = {KeyAlgo}");
            if (KeyType != KeyType.Public) throw new ApplicationException($"Incompatible KeyType = {KeyAlgo}");
            var res = HybridEcMlKemPublicKey.Create(this.KeyBytes);
            return res;
        }

        public HybridEcMlKemPrivateKey ToHybridEcMlKemPrivateKey()
        {
            if (KeyAlgo != KeyAlgo.HybridEcMlKem) throw new ApplicationException($"Incompatible KeyAlgo = {KeyAlgo}");
            if (KeyType != KeyType.Private) throw new ApplicationException($"Incompatible KeyType = {KeyAlgo}");
            var res = HybridEcMlKemPrivateKey.Create(this.KeyBytes);
            return res;
        }

        public static CryptoKey CreateAes(byte[] bytes)
        {
            var res = new CryptoKey();
            res.KeyAlgo = KeyAlgo.Aes;
            res.KeyType = KeyType.Secret;
            res.KeyBytes = GPB.ByteString.CopyFrom(bytes);
            return res;
        }

        public override DataType GetDataType()
        {
            return DataType.CryptoKey;
        }

        public string Explain()
        {
            return $"KeyBytes.Hash:'{KeyBytes?.GetHashCode()}', KeyAlgo:'{KeyAlgo}'";
        }
    }
}
