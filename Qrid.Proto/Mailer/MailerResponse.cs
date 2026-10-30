using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Mailer
{
    public partial class MailerResponse : Qrid.Common.Proto.ProtoBase
    {

        public static MailerResponse Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static MailerResponse Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static MailerResponse Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }


    }
}
