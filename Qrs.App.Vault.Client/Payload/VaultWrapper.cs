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
    public class VaultWrapper : Wrapper<Proto.Vault>
    {
        public string FileName { get; set; } // just a file name (no path)
        public string File { get; set; } // file path in OS
        
        
        // do we need these 3 below ???
        public bool IsNew { get; set; } // for new files, need to create New Key when saving them
        public bool IsModified { get; set; } // has Data been modified
        public bool IsAssociatedDataModified { get; set; } // has AssociatedData been modified (and so we need QR code roundtrip on Save)

        public string ErrorMessage { get; set; }

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        public VaultWrapper(string appName) : base(appName) 
        {
            //Payload = new Proto.Vault();
            
        }

        public override string GetPayloadType() => "mime:application/vnd.qrs.vault";

        public override int GetImmutableTopLevelKey() => 1;

        // When an already-open vault is edited in memory and saved again, reuse the DEK that was
        // unwrapped when the vault was opened instead of generating a new one. This avoids a QR
        // code round-trip to the QRS app on every save, so the vault can stay open while editing.
        // it is still secure b/c on every CryptoUtils.EncryptWithAesGcm(...) we generate a new random Nonce,
        // so the AES key stream is different even though DEK key is the same.
        public override bool GenerateNewDekOnModifiedPayload => false;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.KekName = string.Empty; // we dont know the KekName, it will be set inside QRS Mobile app
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "Title", Value = FileName });
            return res;
        }

    }
}
