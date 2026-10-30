using Google.Protobuf;
using System;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrid.Common.Proto.CanonicalCrypto
{
    public partial class EccPrivateKey : CanonicalKey
    {

        public static EccPrivateKey Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EccPrivateKey Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EccPrivateKey Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public static EccPrivateKey Create(CR.CryptoKey privKey)
        {
            var res = Parser.ParseFrom(privKey.KeyBytes);
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
                return CR.KeyType.Private;
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
