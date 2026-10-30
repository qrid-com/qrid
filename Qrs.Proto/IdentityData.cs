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
    public partial class IdentityData : Qrid.Common.Proto.ProtoBase
    {

        public static IdentityData Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Create<IdentityData>(bytes);
            return res;
        }

        public static IdentityData Create(byte[] bytes)
        {
            var res = Create<IdentityData>(bytes);
            return res;
        }

        public static IdentityData Create(ByteString byteString)
        {
            var res = Create<IdentityData>(byteString);
            return res;
        }

        private void AppendDifference(IdentityData source, StringBuilder diff)
        {
            Utils.StringDifference(diff, "AppName", this.AppName, source.AppName);
            Utils.StringDifference(diff, "DekId", this.DekId.ToBase64(), source.DekId.ToBase64());
            Utils.StringDifference(diff, "TopLevelKey", this.TopLevelKey.ToString(), source.TopLevelKey.ToString());
            Utils.StringDifference(diff, "Created", this.Created?.ToString(), source.Created?.ToString());
        }

        public string DifferenceWith(IdentityData source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifference(source, diff);
            return diff.ToString();
        }

        public string InvalidDifferenceWith(IdentityData source)
        {
            // difference in these fields is generally not allowed when RESEALING
            var diff = new StringBuilder(string.Empty);
            AppendDifference(source, diff);
            return diff.ToString();
        }

    }
}