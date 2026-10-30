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
    public partial class AssociatedDataDek : Qrid.Common.Proto.ProtoBase
    {

        public static AssociatedDataDek Create(string base64string)
        {
            if (string.IsNullOrWhiteSpace(base64string)) return null;
            // do NOT try catch, let them see the exception here 
            var bytes = Convert.FromBase64String(base64string);
            // or here
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AssociatedDataDek Create(byte[] bytes)
        {
            var res = Parser.ParseFrom(bytes);
            return res;
        }

        public static AssociatedDataDek Create(ByteString byteString)
        {
            var res = Parser.ParseFrom(byteString);
            return res;
        }

        public void CopyCarriedOverFields(AssociatedDataDek source)
        {
            this.KekName = source.KekName;
            this.TopLevelKey = source.TopLevelKey;
            this.Created = source.Created;
        }

        
        private void AppendDifferenceInItems(AssociatedDataDek source, StringBuilder diff)
        {
            var thisItems = this.AssociatedDataItems.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            var sourceItems = source.AssociatedDataItems.ToDictionary(x => x.Key, x => x.Value ?? string.Empty);
            Utils.DictionaryDifference(diff, thisItems, sourceItems);
        }

        private void AppendDifferenceInAdDek(AssociatedDataDek source, StringBuilder diff)
        {
            Utils.StringDifference(diff, "AppName", this.AppName, source.AppName);
            Utils.StringDifference(diff, "KekName", this.KekName, source.KekName);
            Utils.StringDifference(diff, "DekId", this.DekId, source.DekId);
            Utils.StringDifference(diff, "TopLevelKey", this.TopLevelKey.ToString(), source.TopLevelKey.ToString());
            Utils.StringDifference(diff, "Created", this.Created?.ToString(), source.Created?.ToString());
        }

        public string DifferenceWith(AssociatedDataDek source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInAdDek(source, diff);
            AppendDifferenceInItems(source, diff);
            return diff.ToString();
        }
        public string DifferenceInAdDekWith(AssociatedDataDek source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInAdDek(source, diff);
            return diff.ToString();
        }
        public string DifferenceInItemsWith(AssociatedDataDek source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInItems(source, diff);
            return diff.ToString();
        }

        public string InvalidDifferenceWith(AssociatedDataDek source)
        {
            // difference in these fields is generally not allowed when RESEALING
            var diff = new StringBuilder(string.Empty);
            AppendDifferenceInAdDek(source, diff);
            return diff.ToString();
        }

        public string DifferenceWithAdPay(AssociatedDataPayload adPay)
        {
            if (adPay == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);

            Utils.StringDifferenceDekPay(diff, "AppName", this.AppName, adPay.AppName);
            Utils.StringDifferenceDekPay(diff, "DekId", this.DekId, adPay.DekId);

            return diff.ToString();
        }

        public string DifferenceWithAdPay(AssociatedDataPayload adPay, string appName)
        {
            // do an extra check comparing AppName (usually from current Context)
            var diff = new StringBuilder(string.Empty);
            Utils.StringDifference(diff, "AppName", appName, this.AppName);
            if (diff.Length > 0)
                return diff.ToString();
            else
                // then do the regular DEK vs PAY diff
                return DifferenceWithAdPay(adPay);
        }


        private void AppendDataDifferenceInAdDek(AssociatedDataDek source, StringBuilder diff)
        {
            Utils.StringDifference(diff, "AppName", this.AppName, source.AppName);
            //Utils.StringDifference(diff, "KekName", this.KekName, source.KekName);
            Utils.StringDifference(diff, "DekId", this.DekId, source.DekId);
            Utils.StringDifference(diff, "TopLevelKey", this.TopLevelKey.ToString(), source.TopLevelKey.ToString());
            //Utils.StringDifference(diff, "Created", this.Created?.ToString(), source.Created?.ToString());
        }

        public string DifferenceInDataWith(AssociatedDataDek source)
        {
            if (source == null) return string.Empty;
            var diff = new StringBuilder(string.Empty);
            AppendDataDifferenceInAdDek(source, diff); // a bit more relaxed check 
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