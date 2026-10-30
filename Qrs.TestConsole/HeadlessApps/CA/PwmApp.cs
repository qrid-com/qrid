using Qrid.Common.Client.Cookie;
using Qrs.App.PasswordManager.Client;
using Qrs.App.PasswordManager.Client.Models;
using Qrs.TestConsole.HeadlessApps.MA;
using Qrs.TestConsole.IntegrationTests;
using Qrs.Tunnel.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.HeadlessApps.CA
{
    // Headless QRS Client Application that exercises Qrs.App.PasswordManager.Client.PwmContext
    public class PwmApp : ClientAppBase
    {
        private string _appRootPath;
        public PwmContext Context => (PwmContext)base.ClientContext;

        // folder where password manager stores "folder_key.qrs1" and "*.qrs2" files
        public string DataFolderPath => Path.Combine(_appRootPath, "pwm-data");

        public PwmApp(string username, string userRootPath)
        {
            _appRootPath = Path.Combine(userRootPath, "pwm");
            HUI = new HeadlessUI(this, username, Config.Domain);
        }

        // creates Context
        public async Task InitAsync()
        {
            var asymKeyItemProvider = new FileAsymKeyItemProvider(_appRootPath);
            var cookieProvider = new FileCookieProvider(_appRootPath);

            var context = new PwmContext(HUI, asymKeyItemProvider, cookieProvider);

            // init Context with the Tunnel Server used by the test environment
            var response = await context.InitFromConfigAsync(string.Empty, Config.TunnelServerBaseUrl);
            if (response.IsError())
            {
                HUI.ShowError($"Failed to initialize PwmContext: {response.Message}");
                return;
            }

            base.ClientContext = context;

            // TODO - default setting for TunnelClient - make it configurable? Now it is hard-coded in Config class
            base.ClientContext.TunnelClient.BaseUrl = Config.TunnelServerBaseUrl;

            Directory.CreateDirectory(DataFolderPath);
        }

        // opens the folder - creates (or unwraps existing) FolderKey via QR code roundtrip with MA
        public async Task<Folder> FolderOpenAsync(MobileApp MA)
        {
            Directory.CreateDirectory(DataFolderPath);

            var scannerTask = MA.PointScannerAt(this);
            var folderTask = Context.FolderOpen(DataFolderPath, string.Empty, SortBy.NameAsc, _appRootPath);

            await Task.WhenAll(folderTask, scannerTask);

            var folder = folderTask.Result;
            if (folder.IsError)
            {
                LogDebug($"ERROR - FolderOpen failed: {folder.ErrorMessage}");
                return null;
            }

            Context.Folder = folder;
            LogDebug($"Success - folder '{DataFolderPath}' is open, it contains {folder.Searched.SourceItems.Count} resource(s)");
            return folder;
        }

        // creates a new resource (in memory only) and fills it in with the given data
        public ResourceInfo ResourceCreate(string resourceName, string username, string password, string secretNotes)
        {
            var ri = Context.ResourceCreate(Context.Folder);
            ri.SecretWrapper.CreatePayload(); // creates a blank Secret to fill in

            ri.Name = resourceName;
            ri.Type = "web";
            ri.Username = username;
            ri.Password = password;
            ri.SecretNotes = secretNotes;
            ri.IsSecretOpen = true;

            LogDebug($"Created new resource '{resourceName}' with Id='{ri.Id}'");
            return ri;
        }

        // encrypts and saves the resource - goes through a QR code roundtrip with MA to wrap the new DEK
        public async Task<string> ResourceSaveAsync(ResourceInfo ri, MobileApp MA)
        {
            var scannerTask = MA.PointScannerAt(this);
            var saveTask = Context.ResourceSave(Context.Folder, ri);

            await Task.WhenAll(saveTask, scannerTask);

            var response = saveTask.Result;
            if (response.IsError())
            {
                LogDebug($"ERROR - ResourceSave failed: {response.Message}");
                return null;
            }

            LogDebug($"Success - resource '{ri.Name}' has been encrypted into file: '{ri.QrsFile}'");
            return ri.QrsFile;
        }

        // number of QR codes displayed by this app - used to detect round trips to MA
        public int QrCodeShownCount => HUI.QrCodeShownCount;

        // saves the resource WITHOUT pointing the MA scanner at the app
        // it fails when the save operation requires a QR code roundtrip with MA
        public async Task<string> ResourceSaveNoRoundTripAsync(ResourceInfo ri, int timeoutMs = 60000)
        {
            var qrCountBefore = QrCodeShownCount;

            var saveTask = Context.ResourceSave(Context.Folder, ri);

            var finished = await Task.WhenAny(saveTask, Task.Delay(timeoutMs));
            if (finished != saveTask)
                throw new ApplicationException($"ResourceSave for resource '{ri.Name}' did not complete within {timeoutMs}ms - most likely it is waiting for a QR code roundtrip with the QRS Mobile App which was not expected here");

            if (QrCodeShownCount != qrCountBefore)
                throw new ApplicationException($"ResourceSave for resource '{ri.Name}' has displayed a QR code - a roundtrip to the QRS Mobile App was not expected here");

            var response = await saveTask;
            if (response.IsError())
                throw new ApplicationException($"ResourceSave failed: {response.Message}");

            LogDebug($"Success - resource '{ri.Name}' has been saved into file '{ri.QrsFile}' without a roundtrip to the QRS Mobile App");
            return ri.QrsFile;
        }

        // finds a loaded resource in the currently open folder
        public ResourceInfo GetResource(string resourceId)
        {
            if (!Context.Folder.Searched.SourceItems.TryGetValue(resourceId, out var resourceItem))
                throw new ApplicationException($"Resource with Id='{resourceId}' has not been found in the folder");
            return resourceItem.ResourceInfo;
        }

        // edits the resource the same way the UI does it - only the provided values are modified
        public void ResourceEdit(ResourceInfo ri, string newUsername = null, string newSecretNotes = null)
        {
            if (newUsername != null)
            {
                ri.Username = newUsername;
                ri.IsModified = true;
            }

            if (newSecretNotes != null)
            {
                if (!ri.IsSecretOpen)
                    throw new ApplicationException($"Cannot modify SecretNotes of resource '{ri.Name}' b/c its Secret is not open");
                ri.SecretNotes = newSecretNotes;
                ri.IsModified = true;
                ri.IsSecretModified = true;
            }

            LogDebug($"Edited resource '{ri.Name}' - IsModified={ri.IsModified}, IsSecretModified={ri.IsSecretModified}, IsSecretOpen={ri.IsSecretOpen}");
        }

        // decrypts the resource's Secret - goes through a QR code roundtrip with MA to unwrap the DEK
        public async Task<ResourceInfo> ResourceOpenSecretAsync(string resourceId, MobileApp MA)
        {
            if (!Context.Folder.Searched.SourceItems.TryGetValue(resourceId, out var resourceItem))
            {
                LogDebug($"ERROR - resource with Id='{resourceId}' has not been found in the folder");
                return null;
            }

            var ri = resourceItem.ResourceInfo;

            var scannerTask = MA.PointScannerAt(this);
            var openTask = Context.ResourceOpenSecret(Context.Folder, ri);

            await Task.WhenAll(openTask, scannerTask);

            var response = openTask.Result;
            if (response.IsError())
            {
                LogDebug($"ERROR - ResourceOpenSecret failed: {response.Message}");
                return null;
            }

            LogDebug($"Success - secret has been decrypted for resource '{ri.Name}'");
            return ri;
        }

        // verifies that the decrypted resource is matching the original data
        public string VerifyResource(ResourceInfo ri, string resourceName, string username, string password, string secretNotes)
        {
            if (ri == null)
                return "Resource is null - nothing to verify";

            if (!ri.IsSecretOpen)
                return $"Secret is not open for resource '{ri.Name}'";

            if (ri.Name != resourceName)
                return $"ResourceName does not match. Actual: '{ri.Name}'; Expected: '{resourceName}'";

            if (ri.Username != username)
                return $"Username does not match. Actual: '{ri.Username}'; Expected: '{username}'";

            if (ri.Password != password)
                return $"Password does not match for resource '{ri.Name}'";

            if (ri.SecretNotes != secretNotes)
                return $"SecretNotes do not match for resource '{ri.Name}'";

            LogDebug($"SUCCESS - decrypted resource '{ri.Name}' is matching the original data!");
            return string.Empty;
        }

        // deletes the resource and its .qrs2 file
        public async Task<string> ResourceDeleteAsync(ResourceInfo ri)
        {
            return await Context.ResourceDelete(Context.Folder, ri);
        }

        public void DeleteDataFolder()
        {
            if (Directory.Exists(DataFolderPath))
                Directory.Delete(DataFolderPath, true);
        }
    }
}
