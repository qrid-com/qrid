using Google.Protobuf;
using Qrid.Common.Proto.Crypto;
using System;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class HybridEcMlKemPublicKey : ProtoBase
    {

        public static HybridEcMlKemPublicKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HybridEcMlKemPublicKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static HybridEcMlKemPublicKey Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static HybridEcMlKemPublicKey CreateWith(CryptoKey eccPublicKey, CryptoKey mlKemPublicKey)
        {
            // todo - validate params - is it private? expected algo?
            var res = new HybridEcMlKemPublicKey();
            res.EccPublicKey = eccPublicKey;
            res.MlKemPublicKey = mlKemPublicKey;
            return res;
        }

    }
}