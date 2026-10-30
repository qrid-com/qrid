using Qrid.Common.Data;
using Qrid.Common.Http;
using Qrs.Mobile.Client;
using Qrs.Mobile.Client.Context;
using Qrs.Mobile.Client.Flows;
using Qrs.Proto.Api;
using Qrs.TestConsole.HeadlessApps.CA;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Qrs.TestConsole.HeadlessApps.MA
{
    public class MobileApp
    {
        private string _username;

        public string BackupPassword { get; set; }
        public string BackupImage { get; set; }
        public string BackupMasterKeyName { get; set; }

        

        public QrsAppContext Context { get; set; }
        public bool IsInitialized { get; set; }

        // number of "keyop" requests processed by this Mobile App - used by tests to verify round trips
        public int KeyOpRequestCount { get; private set; }

        // KeyOp types (and their counts) of the last processed "keyop" request
        public Dictionary<KeyOp, int> LastKeyOpCounts { get; private set; } = new Dictionary<KeyOp, int>();

        // KeyOp types present in the last processed "keyop" request
        public List<KeyOp> LastKeyOps => LastKeyOpCounts.Where(kv => kv.Value > 0).Select(kv => kv.Key).ToList();

        public string LastKeyOpsString => string.Join(",", LastKeyOps);

        public MobileApp(string username)
        {
            _username = username;
            var tdi = new TestDeviceImpl(username);
            Context = new QrsAppContext(tdi, null); // TODO - init proper cookie provider instead of null
            IsInitialized = false;
        }

        public async Task InitAsync()
        {
            if (IsInitialized) return;

            Context.Init();

            IsInitialized = true;
        }

        public void CopyBackupData(MobileApp oldMA)
        {
            BackupImage = oldMA.BackupImage;
            BackupPassword = oldMA.BackupPassword;
            BackupMasterKeyName = oldMA.BackupMasterKeyName;
        }

        public async Task CreateNewMasterKeyAsync(string masterKeyName)
        {
            await InitAsync();

            string username = _username;
            string domain = Config.Domain;
            string password = Config.TestMasterKeyPassword;

            // Step 1 - create new MasterKey - CPU bound
            var response1 = await Task.Run(async () => await Context.NewMasterKeyAsync(password, masterKeyName));
            if (!response1)
                LogThrow($"NewMasterKeyAsync failed: returned false");

            // Step 2 - backup MK with password
            BackupImage = Context.GetQrCodeString(Context.NewUserFlow.PasswordEncryptedMasterKey, Context.NewUserFlow.KeyName);
            BackupPassword = password;
            BackupMasterKeyName = masterKeyName;

            // Step 3 - Save MK (held in the OrangeKeys object)
            MobUtils.SaveOrangeKeys(Context.OrangeKeys);
            LogDebug($"CreateNewMasterKeyAsync completed for MasterKey='{masterKeyName}'");
        }

        public void Logout()
        {
            Context.Logout();
            IsInitialized = false;
            LogDebug("Logout completed");
        }

        public async Task RestoreExistingMasterKeyAsync()
        {
            await InitAsync();

            string username = _username;
            string domain = Config.Domain;

            // Step 1 - ParseBackupImage

            var qbi = Context.ParseBackupImage(BackupImage);
            if (!qbi.IsValidBackupImage)
                LogThrow($"BackupImage - IsValidBackupImage = false");
            Context.ExistingUserFlow.QrBackupImage = qbi;

            // Step 2 - use password to decrypt Master Key from Backup Image
            var result = await Task.Run(async () => await Context.ExistingMasterKeyAsync(BackupPassword,
                Context.ExistingUserFlow.QrBackupImage.Key,
                BackupMasterKeyName));

            if (!result)
                LogThrow("Provided Password might be wrong - unexpected error while trying to load your Master Key");

            // Step 3 - Save newly restored Master Key that is being held in OrangeKeys object

            MobUtils.SaveOrangeKeys(Context.OrangeKeys);
            LogDebug($"RestoreExistingMasterKeyAsync completed for MasterKey='{BackupMasterKeyName}'");
        }

        
        private static readonly TimeSpan DefaultScanTimeout = TimeSpan.FromSeconds(30);

        public async Task PointScannerAt(ClientAppBase clientApp)
        {
            await InitAsync();

            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            clientApp.HUI.AwaitingScannerTasks.Add(tcs);
            var qrString = await tcs.Task; // TODO: add timeout/cancellation support
            await Task.Delay(600); // sometimes code shows QR Window before actually submitting to Server, so we should NOT rush to fetch from server, let's wait 0.2 sec just in case
            await ProcessScannedQrCodeAsync(qrString, clientApp);
        }

        public void ShowError(string message)
        {
            LogDebug($"ERROR: {message}");
        }

        public void LogDebug(string message)
        {
            Context.LogDebug(message);
        }

        public async Task ProcessScannedQrCodeAsync(string qrString, ClientAppBase clientApp)
        {
            await InitAsync();

            var qrCmd = QrCommand.Create(qrString);

            // Download Request
            await Context.DownloadRequestAsync(qrCmd);

            if (Context.QrCommandFlow.HasError())
                LogThrow(new ApplicationException(Context.QrCommandFlow.ErrorMessage));

            if (Context.QrCommandFlow.HasPairing())
            {
                // pair ClientApp with us
                clientApp.HUI.UiPairingCode = Context.QrCommandFlow.PairingRequest.PairingCode;
                
                // await payload after pairing
                await Context.DownloadRequestAfterPairingAsync();

                if (Context.QrCommandFlow.HasError())
                    LogThrow(new ApplicationException(Context.QrCommandFlow.ErrorMessage));
            }

            if (!Context.QrCommandFlow.HasPayload())
                LogThrow(new ApplicationException("Unexpected state - no pairing, no error, no payload"));


            // TODO - WTF??? flow in the QRID Mobile App - does it do this?
            // ???????????????????????? this make the test working
            Context.QrCommandFlow.ParsePayload(Context.OrangeKeys.KeyName);



            // Process the command and send the response back to the CA
            await PerformCommandAndRespondAsync();
        }

        private async Task PerformCommandAndRespondAsync()
        {
            var cmd = Context.QrCommandFlow.QrCommand.Cmd;
            LogDebug($"PerformCommandAndRespondAsync - cmd='{cmd}'");

            switch (cmd)
            {
                case "keyop":
                    KeyOpRequestCount++;
                    LastKeyOpCounts = new Dictionary<KeyOp, int>(Context.QrCommandFlow.KeyOpCount);
                    var keyopResponse = Context.KeyOp();
                    var sendResponse = await Context.QrCommandSendResponseAsync(keyopResponse);
                    if (!sendResponse.IsSuccess())
                        LogThrow(new ApplicationException($"QrCommandSendResponseAsync failed for cmd='{cmd}': {sendResponse.Message}"));
                    LogDebug($"PerformCommandAndRespondAsync - keyop response sent successfully");
                    break;

                default:
                    LogThrow(new ApplicationException($"QR code contains unsupported cmd='{cmd}'"));
                    break;
            }
        }

        protected void LogThrow(Exception ex)
        {
            LogDebug($"ERROR - - - {ex.Message}");
            throw ex;
        }

        protected void LogThrow(string exceptionMessage)
        {
            LogDebug($"ERROR - LogThrow(string) - - - {exceptionMessage}");
            throw new ApplicationException(exceptionMessage);
        }
    }
}
