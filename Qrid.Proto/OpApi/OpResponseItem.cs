using Google.Protobuf;
using Qrid.Common.Proto;
using System;
using System.Collections.Generic;
using System.Text;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.OpApi
{
    public partial class OpResponseItem : Qrid.Common.Proto.ProtoBase
    {

        public static OpResponseItem Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static OpResponseItem Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static OpResponseItem Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }


        public bool IsError()
        {
            return !string.IsNullOrWhiteSpace(ErrorMessage);
        }

        public byte[] IdBytes => IdentityDataBytes.ToByteArray(); // ByteString cannot be null here

        public byte[] NewAdBytes => NewAssociatedDataBytes.ToByteArray(); // ByteString cannot be null here

        public byte[] NewAdIdBytes => Utils.Combine(NewAdBytes, IdBytes);

    }
}