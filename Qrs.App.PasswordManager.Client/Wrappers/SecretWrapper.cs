using Qrid.Common.Http;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.Client.Context;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.App.PasswordManager.Client.Proto;
using Qrs.Proto;

namespace Qrs.App.PasswordManager.Client.Wrappers
{
    public class SecretWrapper : Wrapper<Secret>
    {
        public ResourceInfo ResourceInfo { get; set; } 

        //public string DekId { get;set; }
        
        public SecretWrapper(string appName, ResourceInfo resourceInfo) : base(appName) 
        {
            ResourceInfo = resourceInfo;
        }

        public override string GetPayloadType() => "S";
        public override bool GenerateNewDekOnModifiedPayload => false;

        public override AssociatedData2 GetAssociatedData2()
        {
            var res = new AssociatedData2();
            var rd = ResourceInfo?.ResourceData;
            res.KekName = string.Empty; // we dont know the KekName, it will be set inside QRS Mobile app
            
            if (!string.IsNullOrWhiteSpace(rd.ResourceName))
                res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "Website / Resource", Value = rd.ResourceName });
            if (!string.IsNullOrWhiteSpace(rd.Username))
                res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "Username", Value = rd.Username });
            if (!string.IsNullOrWhiteSpace(rd.FilePath))
                res.AssociatedDataItems.Add(new AssociatedDataItem { Key = "File", Value = rd.FilePath });
            
            return res;
        }
        


    }
}
