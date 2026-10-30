using Google.Protobuf;
using Qrid.Common.Proto.Crypto;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class HybridEcMlKemPrivateKey : ProtoBase
    {

        public static HybridEcMlKemPrivateKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HybridEcMlKemPrivateKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HybridEcMlKemPrivateKey Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static HybridEcMlKemPrivateKey CreateWith(CryptoKey eccPrivateKey, CryptoKey mlKemPrivateKey)
        {
            // todo - validate params - is it private? expected algo?
            var res = new HybridEcMlKemPrivateKey();
            res.EccPrivateKey = eccPrivateKey;
            res.MlKemPrivateKey = mlKemPrivateKey;
            return res;
        }

    }
}