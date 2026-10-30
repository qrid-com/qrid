using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Http;
using Qrs.Discovery.Client;
using Qrs.Proto.Discovery;
using Qrs.Shared.Data;
using Qrs.Tunnel.Shared.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BC = Qrs.Tunnel.Client;
using CR = Qrid.Common.Proto.Crypto;

namespace Qrs.Mobile.Client.Context
{
    public partial class QrsAppContext : CommonContext
    {
        public const string APP_NAME = "QRS";

        public QrsConfiguration Config;
        public BC.TunnelClient TunnelClient { get; set; }
        public Flows.NewUserFlow NewUserFlow { get; set; }
        public Flows.ExistingUserFlow ExistingUserFlow { get; set; }
        public Flows.BackupFlow BackupFlow { get; set; }
        public Flows.QrCommandFlow QrCommandFlow { get; set; }
        
        public Data.OrangeKeys OrangeKeys { get; set; }

        public bool IsLoggedIn => OrangeKeys != null;

        // DeviceKeyItem - contains "paired" Asym Key Pair that belongs to us (to MA) and that CA uses to communicate with us (with MA).
        private BC.AsymKeyItem DeviceKeyItem;

        public ICookieProvider CookieProvider { get; set; }

        public QrsAppContext(Device.IDevice device, ICookieProvider cookieProvider)
        {
            MobUtils.Device = device;
            CookieProvider = cookieProvider;
            
            ResetFlows();
        }

        // discards all the data held by the UI flows (new key, existing key, backup, qr command)
        public void ResetFlows()
        {
            NewUserFlow = new Flows.NewUserFlow();
            ExistingUserFlow = new Flows.ExistingUserFlow();
            BackupFlow = new Flows.BackupFlow();
            QrCommandFlow = new Flows.QrCommandFlow();
        }

        public bool Init()
        {
            OrangeKeys = MobUtils.LoadOrangeKeys();
            // load or generate new DeviceKeyItem (new will result into Pairing flow on its first use, which is ok)
            DeviceKeyItem = MobUtils.LoadOrGenerateDeviceKeyItem(); 
            CreateTunnelClient();
            return true;
        }

        private void CreateTunnelClient()
        {
            TunnelClient = new BC.TunnelClient(Directions.MA2CA, APP_NAME, CookieProvider);
        }

        // Init performs secure storage access and (potentially) key generation, which are too slow
        // to run on the UI thread - on iOS that leads to the app being killed by the startup watchdog.
        public Task<bool> InitAsync()
        {
            return Task.Run(() => Init());
        }

        public void ResetPairing()
        {
            DeviceKeyItem = MobUtils.ReGenerateDeviceKeyItem();
        }

        public async Task<HttpResponseModel<StringResponse>> InitDomainAsync(string domain)
        {
            var discovered = await DiscoveryClient.Instance.GetDomainQrsConfigurationAsync(domain);
            if (!discovered.IsSuccess()) { return ErrorResponse<StringResponse>(discovered); }

            var res = InitDomain(discovered.Data.QrsConfiguration);
            return res ? SuccessStringResponse(null) : ErrorStringResponse($"Domain '{domain}' supports QRS protocols but QRS client initialization has failed");
        }


        public bool InitDomain(QrsConfiguration qridConfig)
        {
            Reset();
            
            
            TunnelClient = new BC.TunnelClient(Directions.MA2CA, APP_NAME, CookieProvider);
            TunnelClient.BaseUrl = Config.TunnelServiceUri;

            return true;
        }

        public void Reset()
        {
            TunnelClient = null;
        }

        public string GetQrCodeString(CR.PasswordEncryptedBytes encryptedKey, string KeyName)
        {
            var qbi = new QrBackupImage();
            // "key" first, then "name"
            qbi.Key = encryptedKey.GetBase64String();
            qbi.Name = KeyName;
            return qbi.ToQrString();
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - {message}");
            Debug.WriteLine($"{DateTime.Now.ToString()} - {message}");
        }

    }
}
