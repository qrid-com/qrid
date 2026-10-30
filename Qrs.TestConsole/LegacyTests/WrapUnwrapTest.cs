using Qrid.Common.Client.Cookie;
using Qrid.Common.Crypto;
using Qrs.Client.Context;
using Qrs.Proto;
using Qrs.Tunnel.Client;
using Qrs.App.Vault.Client.Payload;
using System.IO;
using System.Threading.Tasks;
using Qrs.TestConsole.HeadlessApps.MA;


namespace Qrs.TestConsole.LegacyTests
{
    public class WrapUnwrapTest : TestBase
    {
        
        public async Task<bool> Run()
        {

            PerformAsymPqCryptoTest();


            var asymKeyItemProvider = new FileAsymKeyItemProvider(Directory.GetCurrentDirectory());
            var cookieProvider = new MemoryCookieProvider();
            LogDebug($"FileAsymKeyItemProvider initialized with Folder='{asymKeyItemProvider.Folder}'");
            var testDevice = new TestDeviceImpl();
            var clientContext = new ClientContext("TEST", this, asymKeyItemProvider, cookieProvider);
            await clientContext.InitFromConfigAsync("", "http://localhost:4999/");
            Init(testDevice, clientContext);

            // this will throw if not up
            await EnsureServiceUp(40);

            var dek = CryptoUtils.GetRandomSymKey();
            var ad = new AssociatedDataDek();
            // todo - fill in ad with data

            // wrapper obj
            var w = new TestPayloadWrapper("TEST");
            w.Username = "test-user-123";

            // WRAP PAYLOAD
            var wrapKeyTask = CaContext.WrapNewPayload(w); // .WrapKeyAsync(dek, ad);
            var qrScanTask = MaPerformWrapKey();

            // wait for both tasks to finish
            Task.WaitAll(wrapKeyTask, qrScanTask);

            if (!wrapKeyTask.IsCompleted)
                LogThrow($"wrapKeyTask has not completed");

            if (!wrapKeyTask.Result.IsSuccess())
                LogThrow($"wrapKeyTask completed with error - {wrapKeyTask.Result.Message}");

            LogDebug("WrapKey command has completed successfully");

            // do we need to RESET contexts/flows here?

            // UNWRAP PAYLOAD
            var unwrapKeyTask = CaContext.UnwrapPayload(w);

            var unwrapQrScanTask = MaPerformUnwrapKey();

            // wait for both tasks to finish
            Task.WaitAll(unwrapKeyTask, unwrapQrScanTask);

            if (!unwrapKeyTask.IsCompleted)
                LogThrow($"unwrapKeyTask has not completed");

            if (!unwrapKeyTask.Result.IsSuccess())
                LogThrow($"unwrapKeyTask completed with error - {unwrapKeyTask.Result.Message}");

            LogDebug("UnwrapKey command has completed successfully");

            // TODO - compare wrapped payload vs unwrapped payload once comparison baseline is implemented.
            LogDebug("SUCCESS - wrapped and then unwrapped PAYLOAD is the same as original PAYLOAD");

            return true; // test passed
        }


        public void PerformAsymCryptoTest()
        {
            var senderCkp = CryptoUtils.GetRandomAsymCryptoKeyPair();
            var receiverCkp = CryptoUtils.GetRandomAsymCryptoKeyPair();

            var data = new byte[4];
            data[0] = 1;
            data[1] = 2;
            data[2] = 3;
            data[3] = 4;


            var aebData = CryptoUtils.EncryptAsym(data, receiverCkp.PublicKey);

            var data2 = CryptoUtils.DecryptAsym(aebData, receiverCkp.PrivateKey);

        }

        public void PerformAsymPqCryptoTest()
        {
            var senderCkp = CryptoUtils.GetRandomAsymCryptoKeyPair(true);
            var receiverCkp = CryptoUtils.GetRandomAsymCryptoKeyPair(true);

            var data = new byte[4];
            data[0] = 5;
            data[1] = 6;
            data[2] = 7;
            data[3] = 8;


            var aebData = CryptoUtils.EncryptAsymHybrid(data, receiverCkp.PublicKey);

            var data2 = CryptoUtils.DecryptAsymHybrid(aebData, receiverCkp.PrivateKey);

        }

        public async Task MaPerformWrapKey()
        {

            await PerformMobileQrCodeScan();

            if (MaContext.QrCommandFlow.HasPayload())
            {
                var wrapResponse = MaContext.KeyOp();
                var sendWrapResponse = await MaContext.QrCommandSendResponseAsync(wrapResponse);
                if (!sendWrapResponse.IsSuccess())
                {
                    // UI - show error
                    ShowError(sendWrapResponse.Message);
                    return;
                }
                // success ???
                return;
            }

            if (MaContext.QrCommandFlow.HasError())
            {
                // UI - show error
                ShowError(MaContext.QrCommandFlow.ErrorMessage);
                return;
            }

            //???
        }

        public async Task MaPerformUnwrapKey()
        {

            await PerformMobileQrCodeScan();

            if (MaContext.QrCommandFlow.HasPayload())
            {
                var unwrapResponse = MaContext.KeyOp();
                var sendUnwrapResponse = await MaContext.QrCommandSendResponseAsync(unwrapResponse);
                if (!sendUnwrapResponse.IsSuccess())
                {
                    // UI - show error
                    ShowError(sendUnwrapResponse.Message);
                    return;
                }
                // success ???
                return;
            }

            if (MaContext.QrCommandFlow.HasError())
            {
                // UI - show error
                ShowError(MaContext.QrCommandFlow.ErrorMessage);
                return;
            }

            //???
        }



    }
}
