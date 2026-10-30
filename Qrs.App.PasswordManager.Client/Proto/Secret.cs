using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.App.PasswordManager.Client.Proto
{
    public partial class Secret : Qrid.Common.Proto.ProtoBase
    {

        public static Secret Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Secret Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Secret Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public bool IsValidPadding()
        {
            if (string.IsNullOrEmpty(Padding)) return true;
            var notp = Padding.Replace("p", "");
            return string.IsNullOrEmpty(notp);
        }

    }
}