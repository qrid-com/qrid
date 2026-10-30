using Qrs.Mobile.Client.Flows;
using System.Threading.Tasks;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.Mobile.Client.Context
{
    public partial class QrsAppContext 
    {

        public async Task<bool> BackupMasterKeyAsync(string password)
        {
            // encrypt - use KeyName as AssociatedData(AEAD) when encrypting MasterKey into QR code
            var passwordEncryptedMasterKey = CryptoUtils.PasswordEncryptCryptoKey(OrangeKeys.MasterKeySym, password,
                Qrid.Common.Proto.StringObject.CreateWithString(OrangeKeys.KeyName));
            // save Command-format QR string (embeds Key Name)
            BackupFlow.QrCodeString = GetQrCodeString(passwordEncryptedMasterKey, OrangeKeys.KeyName);
            return true;
        }

    }
}
