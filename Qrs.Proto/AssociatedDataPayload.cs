using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Google.Protobuf;
using Qrid.Common.Proto;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrs.Proto
{
    public partial class AssociatedDataPayload : Qrid.Common.Proto.ProtoBase
    {

        public static AssociatedDataPayload Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AssociatedDataPayload Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AssociatedDataPayload Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public static AssociatedDataPayload Create(string appName, string dekId)
        {
            return new AssociatedDataPayload
            {
                AppName = appName,
                DekId = dekId
            };
        }

        
        public string DifferenceWith(AssociatedDataPayload source)
        {
            var diff = new StringBuilder(string.Empty);
            Utils.StringDifference(diff, "AppName", this.AppName, source.AppName);
            Utils.StringDifference(diff, "DekId", this.DekId, source.DekId);
            return diff.ToString();
        }
    }
}