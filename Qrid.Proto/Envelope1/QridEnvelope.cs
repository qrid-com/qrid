using Google.Protobuf;
using Qrid.Common.Proto;
using System;
using System.Collections.Generic;
using System.Text;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Envelope1
{
    public partial class QridEnvelope : Qrid.Common.Proto.ProtoBase
    {

        public static QridEnvelope Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static QridEnvelope Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static QridEnvelope Create(ByteString byteString)
        {
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public byte[] IdBytes => IdentityDataBytes.ToByteArray(); // ByteString cannot be null here

        public byte[] AdBytes => AssociatedDataBytes.ToByteArray(); // ByteString cannot be null here

        public byte[] AdIdBytes => Utils.Combine(AdBytes, IdBytes);

        public AssociatedData GetAssociatedData()
        {
            return AssociatedData.Create(AssociatedDataBytes);
        }
        public IdentityData GetIdentityData()
        {
            return IdentityData.Create(IdentityDataBytes);
        }

    }
}