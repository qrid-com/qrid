using Qrid.Common.Crypto;
using Qrid.Common.Proto;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.App.Qrs.Cli.Models;
using Qrs.Client.Context;
using Qrs.Proto;
using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.App.Qrs.Cli.Payload
{
    public class CommandKeyWrapper : Wrapper<CryptoKey>
    {
        public CommandFile CommandFile { get; set; } // content of the YML file

        public string DekId { get;set; }
        
        public CommandKeyWrapper(ClientContext context, CommandFile commandFile) : base(context.AppName) 
        { 
            CommandFile = commandFile;
        }

        public override int GetImmutableTopLevelKey() => 1;

        public override string GetPayloadType() => "ck";

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.KekName = string.Empty;
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "File Title", Value = CommandFile.Title });
            return res;
        }

        public byte[] GetAssociatedDataForEnvarItem(string envarName)
        {
            // I think we only need "varname" here. To protect from "attacker" swapping the 2 envvar values within the *SAME* file
            // we dont need AppName, DekId here b/c this is used with a unique SymKey that only exists in a single file

            // so, let's just use Utf8 encoded string as AD here
            return Encoding.UTF8.GetBytes(envarName);
        }

    }
}
