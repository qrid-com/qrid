using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CertifiedSig : ProtoBase
    {



        public static CertifiedSig Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static CertifiedSig Create(HashSig sig, Certificate certificate)
        {
            return new CertifiedSig() { SigCertificate = certificate, HashSig = sig };
        }


        //public bool ComparePublicKeys()
        //{
        //    // return true if PubKey in a HashSig is the same as PubKey in a Certificate

        //    var certifiedPubKey = SigCertificate?.PublicKey;
        //    this.HashSig.

        //    return true;
        //}


        public string Explain()
        {
            return $"HashSig:[{HashSig?.Explain()}], SigCertificate:[ {SigCertificate?.Explain()} ]";
        }


    }
}