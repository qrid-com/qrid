using System;

namespace Qrid.Common.Proto.Crypto
{
    public partial class CryptoSig : ProtoBase
    {

        public static CryptoSig Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        //public CryptoKey PublicKey 
        //{
        //    get
        //    {
        //        var sig = EcDsaSignature.Create(this.SigBytes);
        //        sig.
        //    }
        //}

        public string Explain()
        {
            return $"SigType:{SigType}, SigBytes.Hash:'{SigBytes?.GetHashCode()}'";
        }


    }
}