using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Mail
{
    public partial class AttachmentHeader : Qrid.Common.Proto.ProtoBase
    {

        public static AttachmentHeader Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AttachmentHeader Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AttachmentHeader Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public string SizeStr()
        {
            if (FileSize < 1024)
                return $"{FileSize} b";
            if (FileSize < 1024*1024)
                return $"{FileSize / 1024} Kb";

            return $"{FileSize / (1024 * 1024)} Mb";
        }

    }
}