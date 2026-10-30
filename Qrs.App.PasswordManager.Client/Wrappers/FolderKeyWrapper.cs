using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.Client.Context;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.App.PasswordManager.Client.Proto;
using Qrs.Proto;

namespace Qrs.App.PasswordManager.Client.Wrappers
{
    public class FolderKeyWrapper : Wrapper<CryptoKey>
    {
        public Folder Folder { get; set; } 

        public FolderKeyWrapper(string appName, Folder folder) : base(appName) 
        {
            Folder = folder;
        }

        public override string GetPayloadType() => "F";

        public override int GetImmutableTopLevelKey() => 1;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            res.KekName = string.Empty; // we dont know the KekName, it will be set inside QRS Mobile app
            res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "Folder", Value = $"{Folder.InvariantFolderPath}" });
            return res;
        }

        
    }
}
