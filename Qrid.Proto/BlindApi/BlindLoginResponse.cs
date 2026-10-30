using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.BlindApi
{
    public partial class BlindLoginResponse : Qrid.Common.Proto.ProtoBase
    {

        public static BlindLoginResponse Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static BlindLoginResponse Create(byte[] bytes)
        {
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public CR.CertifiedKeyPair GetKeyApiAccessCertifiedKeyPair()
        {
            return CR.CertifiedKeyPair.Create(KeyApiAccessKeyPair, KeyApiAccessCert);
        }

    }
}
