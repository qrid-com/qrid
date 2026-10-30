using Qrid.Common.Crypto;
using Qrid.Common.Proto;
using Qrid.Common.Utils;
using Qrs.Client.Context;
using Qrs.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.App.FileManager.Client.Payload
{
    public class QrsFileWrapper : Wrapper<StringObject>
    {
        public string FileName { get; set; } // just a file name (no path)
        public string File { get; set; } // file path in OS
        public string Content // plan-text decrypted content
        { 
            get => Payload?.Value; 
            set => Payload.Value = value; 
        } 
        
        // do we need these 3 below ???
        public bool IsNew { get; set; } // for new files, need to create New Key when saving them
        public bool IsModified { get; set; } // has Data been modified
        public bool IsAssociatedDataModified { get; set; } // has AssociatedData been modified (and so we need QR code roundtrip on Save)

        public string ErrorMessage { get; set; }

        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        public QrsFileWrapper(ClientContext context) : base(context.AppName) 
        {
            Payload = new StringObject();
            Payload.Value = null;
        }

        public override string GetPayloadType() => "mime:text/plain;encoding:utf8";
        public override int GetImmutableTopLevelKey() => 1;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "title", Value = FileName });
            //res.Created = SharedUtils.GetTimestampNowTruncatedToMinutes();
            return res;
        }
    }
}
