using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using Qrid.Common.Proto;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.Proto
{
    public partial class QrsEnvelope : Qrid.Common.Proto.ProtoBase
    {

        public static QrsEnvelope Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static QrsEnvelope Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static QrsEnvelope Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public byte[] IdBytes => IdentityDataBytes.ToByteArray(); // ByteString cannot be null here

        public byte[] AdBytes => AssociatedDataBytes.ToByteArray(); // ByteString cannot be null here

        public byte[] AdIdBytes => Utils.Combine(AdBytes, IdBytes);

        public AssociatedData2 GetAssociatedData2()
        {
            return AssociatedData2.Create(AssociatedDataBytes);
        }
        public IdentityData GetIdentityData()
        {
            return IdentityData.Create(IdentityDataBytes);
        }

    }
}