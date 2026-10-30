using Qrid.Common.Crypto;
using Qrid.Common.Proto;
using Qrid.Common.Utils;
using Qrs.Client.Context;
using Qrs.Proto;
using System;
using System.Collections.Generic;
using System.Text;


namespace Qrs.App.Vault.Client.Payload
{
    public class TestPayloadWrapper : Wrapper<StringObject>
    {
        public string Username { get; set; } // just a file name (no path)
        
        
        // do we need these 3 below ???
        public bool IsNew { get; set; } // for new files, need to create New Key when saving them
        public bool IsModified { get; set; } // has Data been modified
        public bool IsAssociatedDataModified { get; set; } // has AssociatedData been modified (and so we need QR code roundtrip on Save)

        public string ErrorMessage { get; set; }

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        public TestPayloadWrapper(string appName) : base(appName) 
        {
            //Payload = new Proto.Vault();
        }

        public override string GetPayloadType() => "mime:application/vnd.qrs.test-payload";

        public override int GetImmutableTopLevelKey() => 1;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.KekName = string.Empty; // we dont know the KekName, it will be set inside QRS Mobile app
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "Username", Value = Username });
            return res;
        }

    }
}
