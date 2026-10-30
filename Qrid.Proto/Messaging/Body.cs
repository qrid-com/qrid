using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Messaging
{
    public partial class Body : Qrid.Common.Proto.ProtoBase
    {

        public static Body Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Body Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Body Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }


        public static Body Create(string contentType, byte[] contentBytes)
        {
            var res = new Body();
            res.ContentBytes = ByteString.CopyFrom(contentBytes);
            res.ContentType = contentType;
            return res;
        }


    }
}