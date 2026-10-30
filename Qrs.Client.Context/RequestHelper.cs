using Qrs.Proto.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.Client.Context
{
    internal class RequestHelper
    {
        // input
        public bool IsModifiedPayload;
        public bool IsModifiedAssociatedData;
        public bool IsDekOpen;
        public bool GenerateNewDekOnModifiedPayload;

        // output
        // 3 different reasons why it may need a roundtrip to MA (to Mobile App)
        public bool NeedsOldKey;
        public bool NeedsNewKey;
        public bool NeedsRewrapWithModifiedAd;

        public bool NeedsMobileApp => NeedsOldKey || NeedsNewKey || NeedsRewrapWithModifiedAd;

        public KeyOp KeyOp => NeedsOldKey || NeedsNewKey ? KeyOp.WrapUpdatedPayload : KeyOp.Reseal;

        public RequestHelper() { }

        public void Calculate()
        {
            NeedsRewrapWithModifiedAd = IsModifiedAssociatedData;
            NeedsNewKey = IsModifiedPayload && GenerateNewDekOnModifiedPayload;
            NeedsOldKey = IsModifiedPayload && (!IsDekOpen);
        }


    }
}
