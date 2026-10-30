using System;
using System.Collections.Generic;
using System.Text;

using Google.Protobuf;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.Proto
{
    public partial class AssociatedData : Qrid.Common.Proto.ProtoBase
    {

        public static AssociatedData Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AssociatedData Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AssociatedData Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static AssociatedData Create(AssociatedDataDek associatedDataDek)
        {
            var res = new AssociatedData();
            res.AssociatedDataType = AssociatedDataType.Dek;
            res.Bytes = associatedDataDek.ToByteString();
            return res;
        }

        public static AssociatedData Create(AssociatedDataPayload associatedDataPayload)
        {
            var res = new AssociatedData();
            res.AssociatedDataType = AssociatedDataType.Payload;
            res.Bytes = associatedDataPayload.ToByteString();
            return res;
        }

        public AssociatedDataDek GetAssociatedDataDek()
        {
            //if (AssociatedDataType != AssociatedDataType.Dek)
            //    throw new InvalidOperationException($"AssociatedDataDek does not support AssociatedDataType = '{AssociatedDataType}', it requires 'DEK'");

            return AssociatedDataDek.Create(Bytes); 
        }

        public AssociatedDataPayload GetAssociatedDataPayload()
        {
            //if (AssociatedDataType != AssociatedDataType.Payload)
            //    throw new InvalidOperationException($"AssociatedDataPayload does not support AssociatedDataType = '{AssociatedDataType}', it requires 'PAY'");

            return AssociatedDataPayload.Create(Bytes);
        }

    }
}