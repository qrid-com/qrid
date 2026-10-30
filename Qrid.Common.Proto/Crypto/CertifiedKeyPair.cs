using Google.Protobuf;
using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CertifiedKeyPair : ProtoBase
    {

        public static CertifiedKeyPair Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CertifiedKeyPair Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static CertifiedKeyPair Create(CryptoKeyPair keyPair, Certificate certificate)
        {
            var res = new CertifiedKeyPair();
            res.KeyPair = keyPair;
            res.Certificate = certificate;
            return res;
        }

        public static CertifiedKeyPair CreateNonCertified(CryptoKeyPair keyPair)
        {
            // put PubKey into Certificate, leave all other things empty in the certificate
            var res = new CertifiedKeyPair();
            res.KeyPair = keyPair;
            res.Certificate = Certificate.Create(keyPair.PublicKey); // seems like this one will do what we want here
            return res;
        }

        public override DataType GetDataType()
        {
            return DataType.CertifiedKeyPair;
        }

    }
}
