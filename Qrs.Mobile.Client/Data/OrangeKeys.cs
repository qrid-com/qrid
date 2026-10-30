using CR = Qrid.Common.Proto.Crypto;

namespace Qrs.Mobile.Client.Data
{
    public class OrangeKeys
    {
        public CR.CryptoKey MasterKeySym { get; set; }
        public string KeyName { get; set; }
    }
}
