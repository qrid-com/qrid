using Qrs.Mobile.Client.Flows;
using Qrs.Shared.Data;
using System;
using System.Threading.Tasks;
using CR = Qrid.Common.Proto.Crypto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.Mobile.Client.Context
{
    public partial class QrsAppContext 
    {

        public async Task<bool> ExistingMasterKeyAsync(string password, string qrCodeString, string keyName, bool useKeyNameAsAssociatedData = true)
        {
            var peb = CR.PasswordEncryptedBytes.Create(qrCodeString);

            // use KeyName as AssociatedData (AEAD) when decrypting the Master Key,
            // or NULL (legacy case when old QR code did not contain key name, it was also created with NULL AEAD, so need to use NULL AEAD when decrypting from such QR codes)
            var associatedData = useKeyNameAsAssociatedData 
                ? Qrid.Common.Proto.StringObject.CreateWithString(keyName).GetByteArray() 
                : null;

            // now decrypt the Master Key
            ExistingUserFlow.OrangeKeysCandidate = new Data.OrangeKeys();
            try
            {
                ExistingUserFlow.OrangeKeysCandidate.MasterKeySym = CryptoUtils.PasswordDecryptCryptoKey(peb, password, associatedData);
            }
            catch (Exception ex)
            {
                return false;
            }
            ExistingUserFlow.OrangeKeysCandidate.KeyName = keyName;

            OrangeKeys = ExistingUserFlow.OrangeKeysCandidate;
            MobUtils.SaveOrangeKeys(OrangeKeys);

            return true;
        }

        public void Logout()
        {
            OrangeKeys = null;
            MobUtils.SecureStorageClear();
            ResetFlows();
            // do not keep the old pairing key in memory - generate (and save) a new one, which results in a new Pairing on next use
            ResetPairing();
            // new TunnelClient - drops the reference to the old pairing key and any tunnel session state
            CreateTunnelClient();
        }

        public QrBackupImage ParseBackupImage(string text)
        {
            var qbi = QrBackupImage.Create(text);
            if (string.IsNullOrWhiteSpace(qbi.Key))
            {
                // old format (without Key Name) - the entire string is the password-encrypted bytes
                qbi = new QrBackupImage();
                qbi.Key = text;
            }
            return qbi;
        }
    }
}
