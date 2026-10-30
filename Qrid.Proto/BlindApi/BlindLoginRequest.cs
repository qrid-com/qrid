using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.BlindApi
{
    public partial class BlindLoginRequest : Qrid.Common.Proto.ProtoBase
    {

        public static BlindLoginRequest Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static BlindLoginRequest Create(byte[] bytes)
        {
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

    }
}
