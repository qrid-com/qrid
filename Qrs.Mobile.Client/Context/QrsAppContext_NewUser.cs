using Qrs.Mobile.Client.Flows;
using System.Threading.Tasks;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.Mobile.Client.Context
{
    public partial class QrsAppContext 
    {
        

        public async Task<bool> NewMasterKeyAsync(string password, string keyName)
        {
            // generate Random Master Key
            NewUserFlow.MasterKeySym = CryptoUtils.GetRandomSymKey();

            // backup seed is exposed in plain text in Backup Image (Qr Code)
            // so we do not want to generate it right after the Master Key. Not really a concern but still
            // so let's do a dummyKey in between Master Key and Backup Seed
            var dummyKey = CryptoUtils.GetRandomSymKey();


            // encrypt for backup
            // use KeyName as AssociatedData(AEAD) when encrypting MasterKey into QR code
            NewUserFlow.PasswordEncryptedMasterKey = CryptoUtils.PasswordEncryptCryptoKey(NewUserFlow.MasterKeySym, password,
                Qrid.Common.Proto.StringObject.CreateWithString(keyName));
            NewUserFlow.KeyName = keyName;

            OrangeKeys = new Data.OrangeKeys();
            OrangeKeys.MasterKeySym = NewUserFlow.MasterKeySym;
            OrangeKeys.KeyName = keyName;
            MobUtils.SaveOrangeKeys(OrangeKeys);

            return true;
        }

    }
}
