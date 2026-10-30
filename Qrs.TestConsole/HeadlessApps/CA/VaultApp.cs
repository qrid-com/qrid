using Qrs.App.Vault.Client;
using Qrs.TestConsole.HeadlessApps.MA;
using Qrs.TestConsole.IntegrationTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qrs.TestConsole.HeadlessApps.CA
{
    public class VaultApp : ClientAppBase
    {
        private string _appRootPath;
        public VaultContext Context => (VaultContext) base.ClientContext;

        public VaultApp(string username, string userRootPath) 
        {
            _appRootPath = userRootPath;
            HUI = new HeadlessUI(this, username, Config.Domain);
            
        }

        // creates Context
        public async Task InitAsync()
        {
            var contextResponse = await VaultContext.CreateInitialized(_appRootPath, HUI);
            if (contextResponse.IsError())
            {
                HUI.ShowError($"Failed to initialize VaultContext: {contextResponse.Message}");
                // TODO: throw? 
                return;
            }

            base.ClientContext = contextResponse.Data;

            // TODO - default setting for TunnelClient - make it configurable? Now it is hard-coded in Config class
            base.ClientContext.TunnelClient.BaseUrl = Config.TunnelServerBaseUrl; 

        }

        public string CreateTestData(string contentName, string secretContent)
        {
            // create content folder
            var contentFolder = Path.Combine(_appRootPath, contentName + ".content");
            // delete existing content folder if exists
            if (Directory.Exists(contentFolder)) 
                Directory.Delete(contentFolder, true);
            // create new content folder
            Directory.CreateDirectory(contentFolder);


            var filePath = Path.Combine(contentFolder, "testData.txt");
            File.WriteAllText(filePath, secretContent);
            LogDebug($"Test file created: '{filePath}' with content: '{secretContent}'");

            return contentFolder;
        }

        public string VerifyTestData(string contentFolder, string secretContent)
        {
            var filePath = Path.Combine(contentFolder, "testData.txt");
            if (!File.Exists(filePath))
                return $"Expected test file does not exist: '{filePath}'";
            
            var content = File.ReadAllText(filePath);
            LogDebug($"Test file read: '{filePath}' with content: '{content}'");

            if (content != secretContent)
                return $"Test file content does not match expected. Actual: '{content}'; Expected: '{secretContent}'";

            // all is good
            LogDebug($"SUCCESS - Test file content is matching the original content!");
            return string.Empty; 
        }

        public async Task<string> SaveIntoVaultAsync(string fileInfo, MobileApp MA)
        {
            var scannerTask = MA.PointScannerAt(this);
            var contentFolder = Path.GetFullPath(fileInfo);
            var vaultTask = Context.VaultContentSaveAndClose(contentFolder);

            await Task.WhenAll(vaultTask, scannerTask);

            var response = vaultTask.Result;

            if (response.IsSuccess())
            {
                LogDebug($"Success - content have been encrypted into a vault file: '{Context.VaultFullFileName}'");
                return Context.VaultFullFileName;
            }
            else
            {
                LogDebug(response.Message);
                return null;
            }
        }

        public async Task<string> ExtractFromVaultAsync(string fileInfo, MobileApp MA)
        {
            var file = File.Exists(fileInfo) || fileInfo.EndsWith(".vault")
                ? fileInfo
                : $"{fileInfo}.vault";

            var scannerTask = MA.PointScannerAt(this);
            var vaultTask = Context.VaultExtract(file);
            await Task.WhenAll(vaultTask, scannerTask);

            var response = vaultTask.Result;

            if (response.IsSuccess())
            {
                var contentFolderPath = Path.GetFullPath(Context.ContentFolder);
                LogDebug($"Success - content has been extracted to folder: '{contentFolderPath}'");
                return contentFolderPath;
            }
            else
            {
                LogDebug(response.Message);
                return null;
            }
        }

    }
}
