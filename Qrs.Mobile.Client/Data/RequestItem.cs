using Qrs.Proto;
using Qrs.Proto.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.Mobile.Client.Data
{
    public class RequestItem
    {
        public KeyOpRequestItem KeyOpRequestItem;
        public KeyOpResponseItem KeyOpResponseItem;

        public IdentityData IdentityData;
        public AssociatedData2 OldAssociatedData;
        public AssociatedData2 NewAssociatedData;
        public AssociatedData2 DisplayAssociatedData => KeyOpRequestItem.KeyOp == KeyOp.Unwrap ? OldAssociatedData : NewAssociatedData;

        public string ErrorMessage;
        public bool IsError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public void Validate()
        {
            // TODO - validate that proper params provided for the given KeyOp type
            switch(KeyOpRequestItem.KeyOp)
            {
                case KeyOp.WrapNewPayload:
                    break;
                default:
                    break;
            }
        }

    }
}
