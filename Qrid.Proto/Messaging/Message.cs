using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;
using Qrid.Common.Proto;

namespace Qrid.Proto.Messaging
{
    public partial class Message : Qrid.Common.Proto.ProtoBase
    {

        public static Message Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Message Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static Message Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public override DataType GetDataType()
        {
            return DataType.Message;
        }


    }
}