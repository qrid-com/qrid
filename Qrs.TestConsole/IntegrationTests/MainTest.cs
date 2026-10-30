using Qrid.Common.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.IntegrationTests
{
    public class MainTest : TestBase
    {

        public MainTest() : base() 
        { 
        }

        public async Task<bool> Run()
        {
            // Work with Bob's apps

            await Bob.MA.InitAsync();

            // Bob creates a new MasterKey in QRS MA
            await Bob.MA.CreateNewMasterKeyAsync("Bobs_MK");

            // Bob inits CA:Vault app
            await Bob.CA.VaultApp.InitAsync();

            // secretContent
            var secretContent = $"Time is: {DateTime.Now.ToString()}; the random secret is: {CryptoUtils.GetRandomPassword(10)}";

            // create test data file for Bob
            var pathToSecretContentFolder = Bob.CA.VaultApp.CreateTestData("BobSecret", secretContent);
            
            LogDebug($"BEFORE ----- Bob created secret content folder with test data for vault: '{pathToSecretContentFolder}'");

            // Bob uses CA:Vault app to encrypt some data
            var vaultFile = await Bob.CA.VaultApp.SaveIntoVaultAsync(pathToSecretContentFolder, Bob.MA);  // should also go through Pairing Flow on a first use of Vault CA
            if (string.IsNullOrWhiteSpace(vaultFile))
                LogThrow("vaultFile is null or whitespace - something went wrong with saving into .vault file");

            LogDebug($"AFTER ----- Bob saved secret content into vault file: '{vaultFile}'");

            // delete the original unencrypted content
            if (Directory.Exists(pathToSecretContentFolder))
                Directory.Delete(pathToSecretContentFolder, true);

            // Bob performs Logout - erases his MasterKey from memory
            Bob.MA.Logout();  

            // Bob closes MA, then re-opens MA
            await Bob.ReOpenMA(Bob.MA);

            // Bob restores his account using "Existing User" flow
            await Bob.MA.RestoreExistingMasterKeyAsync();

            // Bob uses CA:Vault app again to verify his data is still available after restoring the MasterKey

            // Bob ReOpens CA:Vault app (otherwise it refuses to pair one more time in the same session, for security reasons, it only allows pairing as the first messages in the session)
            // would give literally this error:
            // Cannot SWITCH to another PubKey b/c in this session we have already performed 1 successful operations with the existing PubKey. If you still want to switch, please close and re-open this application to start a new session.
            // to avoid that error, need to re-open CA 
            await Bob.ReOpenVaultCA();

            // extract the secret content from the vault - this will be decrypted with the restored MasterKey
            var extractedSecretContentFolder = await Bob.CA.VaultApp.ExtractFromVaultAsync(vaultFile, Bob.MA);

            // verify the extracted content is the same as the original secret content
            var verificationError = Bob.CA.VaultApp.VerifyTestData(extractedSecretContentFolder, secretContent);
            
            if (!string.IsNullOrEmpty(verificationError))
                LogThrow(verificationError);
            
            // clean up - delete content folder and .vault file
            Directory.Delete(extractedSecretContentFolder, true);
            File.Delete(vaultFile);

            // SUCCESS
            return true;
        }
    
    }
}
