using Qrid.Common.Crypto;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.Proto.Api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.IntegrationTests
{
    // Integration test for Qrs.App.PasswordManager.Client.PwmContext
    // covers: FolderOpen (new + existing FolderKey), ResourceCreate, ResourceSave,
    //         ResourceOpenSecret and ResourceDelete - all going through the QRS Tunnel Server
    public class PwmTest : TestBase
    {

        public PwmTest() : base()
        {
        }

        public async Task<bool> Run()
        {
            // Work with Bob's apps

            await Bob.MA.InitAsync();

            // Bob creates a new MasterKey in QRS MA
            await Bob.MA.CreateNewMasterKeyAsync("Bobs_PWM_MK");

            // Bob inits CA:PasswordManager app
            await Bob.CA.PwmApp.InitAsync();

            // start from a clean data folder so the test is repeatable
            Bob.CA.PwmApp.DeleteDataFolder();

            // Bob opens the folder - this creates a new FolderKey and wraps it via MA
            // (this also goes through Pairing Flow on a first use of PasswordManager CA)
            var folder = await Bob.CA.PwmApp.FolderOpenAsync(Bob.MA);
            if (folder == null)
                LogThrow("FolderOpen failed - folder is null");

            // secret data
            var resourceName = "test-website.example";
            var username = $"bob_{CryptoUtils.GetRandomPassword(4)}";
            var password = CryptoUtils.GetRandomPassword(16);
            var secretNotes = $"Time is: {DateTime.Now.ToString()}; the random note is: {CryptoUtils.GetRandomPassword(10)}";

            LogDebug($"BEFORE ----- Bob created a new password manager resource '{resourceName}' for username '{username}'");

            // Bob creates a new resource and saves it - Secret gets encrypted with a new DEK wrapped by MA
            var ri = Bob.CA.PwmApp.ResourceCreate(resourceName, username, password, secretNotes);
            var resourceId = ri.Id;

            var qrsFile = await Bob.CA.PwmApp.ResourceSaveAsync(ri, Bob.MA);
            if (string.IsNullOrWhiteSpace(qrsFile))
                LogThrow("qrsFile is null or whitespace - something went wrong with saving the resource");

            LogDebug($"AFTER ----- Bob saved the resource into file: '{qrsFile}'");

            // Bob performs Logout - erases his MasterKey from memory
            Bob.MA.Logout();

            // Bob closes MA, then re-opens MA
            await Bob.ReOpenMA(Bob.MA);

            // Bob restores his account using "Existing User" flow
            await Bob.MA.RestoreExistingMasterKeyAsync();

            // Bob ReOpens CA:PasswordManager app (otherwise it refuses to pair one more time in the same session, for security reasons)
            await Bob.ReOpenPwmCA();

            // Bob opens the folder again - existing FolderKey gets unwrapped with the restored MasterKey
            var folder2 = await Bob.CA.PwmApp.FolderOpenAsync(Bob.MA);
            if (folder2 == null)
                LogThrow("FolderOpen failed after restoring the MasterKey - folder is null");

            if (!folder2.Searched.SourceItems.ContainsKey(resourceId))
                LogThrow($"Saved resource with Id='{resourceId}' has not been loaded from the folder");

            // Bob decrypts the Secret of the saved resource
            var ri2 = await Bob.CA.PwmApp.ResourceOpenSecretAsync(resourceId, Bob.MA);
            if (ri2 == null)
                LogThrow("ResourceOpenSecret failed - resource is null");

            // verify the decrypted resource is the same as the original one
            var verificationError = Bob.CA.PwmApp.VerifyResource(ri2, resourceName, username, password, secretNotes);

            if (!string.IsNullOrEmpty(verificationError))
                LogThrow(verificationError);

            // Bob deletes the resource
            var deleteError = await Bob.CA.PwmApp.ResourceDeleteAsync(ri2);
            if (!string.IsNullOrEmpty(deleteError))
                LogThrow($"ResourceDelete failed: {deleteError}");

            if (File.Exists(qrsFile))
                LogThrow($"Resource file still exists after delete: '{qrsFile}'");

            if (folder2.Searched.ExistsId(resourceId))
                LogThrow($"Resource with Id='{resourceId}' still exists in the folder after delete");

            // clean up - delete the data folder with the folder key file
            Bob.CA.PwmApp.DeleteDataFolder();

            // SUCCESS
            return true;
        }

        // ---------------------------------------------------------------------------------------
        // TEST CASE 1 - changing Secret Notes (of an already open Secret)
        // this modifies the encrypted Payload only, it does NOT modify the Associated Data,
        // and the DEK is already unwrapped - so it must NOT require a roundtrip to the QRS Mobile App
        // ---------------------------------------------------------------------------------------
        public async Task<bool> RunEditSecretNotesTest()
        {
            LogDebug("=========== PwmTest.RunEditSecretNotesTest - started ===========");

            var r = await CreateResourceAsync("Bobs_PWM_MK_Notes");

            // Bob re-opens the app and unlocks the Secret ("Show" button)
            await ReOpenPwmAppAndFolderAsync();
            var ri = await OpenSecretAsync(r.ResourceId);

            // Bob edits Secret Notes only
            var newSecretNotes = $"EDITED NOTES - time is: {DateTime.Now.ToString()}; the random note is: {CryptoUtils.GetRandomPassword(10)}";
            Bob.CA.PwmApp.ResourceEdit(ri, newSecretNotes: newSecretNotes);

            // Bob saves - NO roundtrip to MA is expected here
            var keyOpsBefore = Bob.MA.KeyOpRequestCount;
            await Bob.CA.PwmApp.ResourceSaveNoRoundTripAsync(ri);

            if (Bob.MA.KeyOpRequestCount != keyOpsBefore)
                LogThrow($"Saving modified Secret Notes has unexpectedly performed {Bob.MA.KeyOpRequestCount - keyOpsBefore} key operation(s) in the QRS Mobile App");

            // Bob re-opens the app and verifies that the new Secret Notes have been saved and are readable
            await ReOpenPwmAppAndFolderAsync();
            var ri2 = await OpenSecretAsync(r.ResourceId);

            VerifyResource(ri2, r.ResourceName, r.Username, r.Password, newSecretNotes);

            await CleanUpAsync(ri2);

            LogDebug("=========== PwmTest.RunEditSecretNotesTest - PASSED ===========");
            return true;
        }

        // ---------------------------------------------------------------------------------------
        // TEST CASE 2 - changing Username while the Secret is NOT open
        // Username is a part of the Associated Data, so the DEK has to be re-wrapped (RESEALED)
        // with the modified Associated Data - this requires a roundtrip to the QRS Mobile App,
        // even though the encrypted Payload itself is not modified at all
        // ---------------------------------------------------------------------------------------
        public async Task<bool> RunEditUsernameSecretClosedTest()
        {
            LogDebug("=========== PwmTest.RunEditUsernameSecretClosedTest - started ===========");

            var r = await CreateResourceAsync("Bobs_PWM_MK_Username");

            // Bob re-opens the app - and does NOT unlock the Secret
            await ReOpenPwmAppAndFolderAsync();
            var ri = Bob.CA.PwmApp.GetResource(r.ResourceId);

            if (ri.IsSecretOpen)
                LogThrow($"Unexpected state - Secret of resource '{ri.Name}' is open while it is expected to be closed");

            // Bob edits the Username only
            var newUsername = $"bob_edited_{CryptoUtils.GetRandomPassword(4)}";
            Bob.CA.PwmApp.ResourceEdit(ri, newUsername: newUsername);

            // Bob saves - a roundtrip to MA IS expected here b/c the Associated Data has been modified
            var keyOpsBefore = Bob.MA.KeyOpRequestCount;
            var qrsFile = await Bob.CA.PwmApp.ResourceSaveAsync(ri, Bob.MA);
            if (string.IsNullOrWhiteSpace(qrsFile))
                LogThrow("ResourceSave failed after modifying the Username while the Secret was closed");

            VerifySingleKeyOpRoundTrip(keyOpsBefore, new List<KeyOp>() { KeyOp.Reseal });

            // Bob re-opens the app and verifies that the new Username has been saved
            // and that the Secret is still readable and unchanged
            await ReOpenPwmAppAndFolderAsync();
            var ri2 = await OpenSecretAsync(r.ResourceId);

            VerifyResource(ri2, r.ResourceName, newUsername, r.Password, r.SecretNotes);

            await CleanUpAsync(ri2);

            LogDebug("=========== PwmTest.RunEditUsernameSecretClosedTest - PASSED ===========");
            return true;
        }

        // ---------------------------------------------------------------------------------------
        // TEST CASE 3 - changing Username AND Secret Notes while the Secret is already open
        // both the Associated Data and the Payload are modified, so a roundtrip to the QRS Mobile App
        // is required - the app may go either with the "new DEK" flow (WrapUpdatedPayload) or with
        // the "reseal existing DEK" flow (Reseal) - both are acceptable here
        // ---------------------------------------------------------------------------------------
        public async Task<bool> RunEditUsernameAndSecretNotesTest()
        {
            LogDebug("=========== PwmTest.RunEditUsernameAndSecretNotesTest - started ===========");

            var r = await CreateResourceAsync("Bobs_PWM_MK_UsernameNotes");

            // Bob re-opens the app and unlocks the Secret ("Show" button)
            await ReOpenPwmAppAndFolderAsync();
            var ri = await OpenSecretAsync(r.ResourceId);

            // Bob edits both the Username (associated data) and the Secret Notes (payload)
            var newUsername = $"bob_edited_{CryptoUtils.GetRandomPassword(4)}";
            var newSecretNotes = $"EDITED NOTES - time is: {DateTime.Now.ToString()}; the random note is: {CryptoUtils.GetRandomPassword(10)}";
            Bob.CA.PwmApp.ResourceEdit(ri, newUsername: newUsername, newSecretNotes: newSecretNotes);

            // Bob saves - a roundtrip to MA IS expected here
            var keyOpsBefore = Bob.MA.KeyOpRequestCount;
            var qrsFile = await Bob.CA.PwmApp.ResourceSaveAsync(ri, Bob.MA);
            if (string.IsNullOrWhiteSpace(qrsFile))
                LogThrow("ResourceSave failed after modifying both the Username and the Secret Notes");

            VerifySingleKeyOpRoundTrip(keyOpsBefore, new List<KeyOp>() { KeyOp.Reseal, KeyOp.WrapUpdatedPayload });

            // Bob re-opens the app and verifies that both the new Username and the new Secret Notes
            // have been encrypted and saved correctly and can be read back
            await ReOpenPwmAppAndFolderAsync();
            var ri2 = await OpenSecretAsync(r.ResourceId);

            VerifyResource(ri2, r.ResourceName, newUsername, r.Password, newSecretNotes);

            await CleanUpAsync(ri2);

            LogDebug("=========== PwmTest.RunEditUsernameAndSecretNotesTest - PASSED ===========");
            return true;
        }

        #region helpers used by the "edit resource" test cases

        // data of the resource created by CreateResourceAsync
        public class CreatedResource
        {
            public string ResourceId { get; set; }
            public string ResourceName { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public string SecretNotes { get; set; }
        }

        // creates a brand new MasterKey, a clean folder and a single saved resource in it
        private async Task<CreatedResource> CreateResourceAsync(string masterKeyName)
        {
            await Bob.MA.InitAsync();

            // Bob creates a new MasterKey in QRS MA
            await Bob.MA.CreateNewMasterKeyAsync(masterKeyName);

            // Bob inits CA:PasswordManager app
            await Bob.CA.PwmApp.InitAsync();

            // start from a clean data folder so the test is repeatable
            Bob.CA.PwmApp.DeleteDataFolder();

            var folder = await Bob.CA.PwmApp.FolderOpenAsync(Bob.MA);
            if (folder == null)
                LogThrow("FolderOpen failed - folder is null");

            var res = new CreatedResource()
            {
                ResourceName = "test-website.example",
                Username = $"bob_{CryptoUtils.GetRandomPassword(4)}",
                Password = CryptoUtils.GetRandomPassword(16),
                SecretNotes = $"Time is: {DateTime.Now.ToString()}; the random note is: {CryptoUtils.GetRandomPassword(10)}"
            };

            var ri = Bob.CA.PwmApp.ResourceCreate(res.ResourceName, res.Username, res.Password, res.SecretNotes);
            res.ResourceId = ri.Id;

            var qrsFile = await Bob.CA.PwmApp.ResourceSaveAsync(ri, Bob.MA);
            if (string.IsNullOrWhiteSpace(qrsFile))
                LogThrow("qrsFile is null or whitespace - something went wrong with saving the resource");

            return res;
        }

        // simulates "close and re-open the Password Manager app", then opens the folder again
        private async Task ReOpenPwmAppAndFolderAsync()
        {
            await Bob.ReOpenPwmCA();

            var folder = await Bob.CA.PwmApp.FolderOpenAsync(Bob.MA);
            if (folder == null)
                LogThrow("FolderOpen failed after re-opening the Password Manager app - folder is null");
        }

        private async Task<ResourceInfo> OpenSecretAsync(string resourceId)
        {
            var ri = await Bob.CA.PwmApp.ResourceOpenSecretAsync(resourceId, Bob.MA);
            if (ri == null)
                LogThrow($"ResourceOpenSecret failed for resource with Id='{resourceId}'");
            return ri;
        }

        private void VerifyResource(ResourceInfo ri, string resourceName, string username, string password, string secretNotes)
        {
            var verificationError = Bob.CA.PwmApp.VerifyResource(ri, resourceName, username, password, secretNotes);
            if (!string.IsNullOrEmpty(verificationError))
                LogThrow(verificationError);
        }

        // verifies that saving has resulted in exactly one roundtrip to MA using one of the allowed KeyOps
        private void VerifySingleKeyOpRoundTrip(int keyOpsBefore, List<KeyOp> allowedKeyOps)
        {
            var roundTrips = Bob.MA.KeyOpRequestCount - keyOpsBefore;
            if (roundTrips != 1)
                LogThrow($"Expected exactly 1 roundtrip to the QRS Mobile App while saving, but there were {roundTrips}");

            var actualKeyOps = Bob.MA.LastKeyOps;
            if (actualKeyOps.Count != 1)
                LogThrow($"Expected exactly 1 KeyOp type in the request sent to the QRS Mobile App, but got: '{Bob.MA.LastKeyOpsString}'");

            if (!allowedKeyOps.Contains(actualKeyOps[0]))
                LogThrow($"QRS Mobile App has been asked to perform KeyOp='{actualKeyOps[0]}' while only these KeyOps are expected here: '{string.Join(",", allowedKeyOps)}'");

            LogDebug($"Saving has performed a single roundtrip to the QRS Mobile App using KeyOp='{actualKeyOps[0]}'");
        }

        // deletes the resource and the whole data folder
        private async Task CleanUpAsync(ResourceInfo ri)
        {
            var deleteError = await Bob.CA.PwmApp.ResourceDeleteAsync(ri);
            if (!string.IsNullOrEmpty(deleteError))
                LogThrow($"ResourceDelete failed: {deleteError}");

            Bob.CA.PwmApp.DeleteDataFolder();
        }

        #endregion

    }
}
