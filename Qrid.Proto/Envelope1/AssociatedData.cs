using Google.Protobuf;
using Qrid.Common.Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CC = Qrid.Common.Proto.CanonicalCrypto;
using CR = Qrid.Common.Proto.Crypto;
using GWK = Google.Protobuf.WellKnownTypes;

namespace Qrid.Proto.Envelope1
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
            var res = Create(byteString.ToByteArray());
            return res;
        }

        public void CopyCarriedOverFields(AssociatedData aDFromEnvelope)
        {
            // do nothing - this does NOT apply to QridEnvelope b/c of the way we split ID and AD in QridEnvelope
        }


        private void AppendDifferenceInItems(AssociatedData source, StringBuilder diff)
        {
            var thisItems = this.AssociatedDataItems.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            var sourceItems = source.AssociatedDataItems.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            Utils.DictionaryDifference(diff, thisItems, sourceItems);
        }

        public string DifferenceWith(AssociatedData source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInItems(source, diff);
            return diff.ToString();
        }

        public string DifferenceInItemsWith(AssociatedData source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInItems(source, diff);
            return diff.ToString();
        }

        public void SetAssociatedDataItems(Dictionary<string, string> associatedDataItems)
        {
            AssociatedDataItems.Clear();
            foreach (var item in associatedDataItems)
            {
                AssociatedDataItems.Add(new AssociatedDataItem() { Key = item.Key, Value = item.Value });
            }
        }

    }
}