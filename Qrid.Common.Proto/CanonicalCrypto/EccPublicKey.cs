using Google.Protobuf;
using System;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class EccPublicKey : CanonicalKey
    {
        public static EccPublicKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EccPublicKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EccPublicKey Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        // var ccKey = CC.EccPublicKey.Parser.ParseFrom(pubKey.KeyBytes);

        public static EccPublicKey Create(CR.CryptoKey pubKey)
        {
            var res = Parser.ParseFrom(pubKey.KeyBytes);
            return res;
        }

        public override ByteString GetByteString()
        {
            return this.ToByteString();
        }

        public override CR.KeyType KeyType
        {
            get
            {
                return CR.KeyType.Public;
            }
        }

        public override CR.KeyAlgo KeyAlgo
        {
            get
            {
                return CR.KeyAlgo.Ecdsa;
            }
        }

    }
}
