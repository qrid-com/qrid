using CR = Qrid.Common.Proto.Crypto;

namespace Qrs.Mobile.Client.Flows
{
    public class NewUserFlow
    {
        public CR.CryptoKey MasterKeySym;
        public CR.PasswordEncryptedBytes PasswordEncryptedMasterKey;
        public string KeyName;

        public Data.OrangeKeys OrangeKeysCandidate;

    }
}
