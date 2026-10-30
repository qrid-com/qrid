using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using Qrid.Common.Proto;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Messaging
{
    public partial class EncryptedHeader : Qrid.Common.Proto.ProtoBase
    {

        public static EncryptedHeader Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EncryptedHeader Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static EncryptedHeader Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public override DataType GetDataType()
        {
            return DataType.EncHeader;
        }
    }
}