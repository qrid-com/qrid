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
    public partial class AssociatedData2 : Qrid.Common.Proto.ProtoBase
    {

        public static AssociatedData2 Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Create<AssociatedData2>(bytes);
            return res;
        }

        public static AssociatedData2 Create(byte[] bytes)
        {
            var res = Create<AssociatedData2>(bytes);
            return res;
        }

        public static AssociatedData2 Create(ByteString byteString)
        {
            var res = Create<AssociatedData2>(byteString);
            return res;
        }

        public void CopyCarriedOverFields(AssociatedData2 source)
        {
            this.KekName = source.KekName;
        }

        
        private void AppendDifferenceInItems(AssociatedData2 source, StringBuilder diff)
        {
            var thisItems = this.AssociatedDataItems.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            var sourceItems = source.AssociatedDataItems.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            Utils.DictionaryDifference(diff, thisItems, sourceItems);
        }

        

        public string DifferenceWith(AssociatedData2 source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInKekName(source, diff);
            AppendDifferenceInItems(source, diff);
            return diff.ToString();
        }

        public string DifferenceInItemsWith(AssociatedData2 source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInItems(source, diff);
            return diff.ToString();
        }

        private void AppendDifferenceInKekName(AssociatedData2 source, StringBuilder diff)
        {
            Utils.StringDifference(diff, "KekName", this.KekName, source.KekName);
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