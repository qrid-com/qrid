using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using Qrid.Common.Proto;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.Proto.Api
{
    public partial class KeyOpRequest : Qrid.Common.Proto.ProtoBase
    {

        public static KeyOpRequest Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static KeyOpRequest Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static KeyOpRequest Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

    }
}