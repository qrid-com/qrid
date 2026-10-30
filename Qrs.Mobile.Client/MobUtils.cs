using Qrid.Common.Proto.Crypto;
using Qrs.Mobile.Client.Data;
using Qrs.Mobile.Client.Device;
using Qrs.Tunnel.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;

namespace Qrs.Mobile.Client
{
    public class MobUtils
    {

        // implementation of IDevice should be provided and initialized by the Mobile App project
        // alternative implementation could be provided by Integration Tests project
        public static IDevice Device { get; set; } 

        public static void SecureStorageSet(string key, string value)
        {
            Device.SecureStorageSet(key, value);
        }

        public static string SecureStorageGet(string key)
        {
            return Device.SecureStorageGet(key);
        }

        public static void SecureStorageClear()
        {
            Device.SecureStorageClear();
        }

        public static void SaveOrangeKeys(OrangeKeys orangeKeys)
        {
            SecureStorageSet("MasterKeySym", orangeKeys?.MasterKeySym?.GetBase64String());
            SecureStorageSet("MasterKeyName", orangeKeys?.KeyName);
        }

        public static OrangeKeys LoadOrangeKeys()
        {
            var res = new OrangeKeys();
            res.KeyName = SecureStorageGet("MasterKeyName");
            res.MasterKeySym = CryptoKey.Create(SecureStorageGet("MasterKeySym"));
            if (res.MasterKeySym == null)
                return null;

            return res;
        }

        public static AsymKeyItem LoadOrGenerateDeviceKeyItem()
        {
            var res = LoadDeviceKeyItem();
            if (res == null)
            {
                res = GenerateRandomDeviceKeyItem();
                SaveDeviceKeyItem(res);
                res = LoadDeviceKeyItem();
                if (res == null)
                    throw new ApplicationException("Cannot save/load newly generated DeviceKeyItem");
            }
            return res;
        }

        public static AsymKeyItem ReGenerateDeviceKeyItem()
        {
            var res = GenerateRandomDeviceKeyItem();
            SaveDeviceKeyItem(res);
            res = LoadDeviceKeyItem();
            if (res == null)
                throw new ApplicationException("Cannot save/load re-generated DeviceKeyItem");
            return res;
        }

        public static AsymKeyItem GenerateRandomDeviceKeyItem()
        {
            var deviceKeyPair = CryptoUtils.GetRandomAsymCryptoKeyPair(postQuantum: true);
            var res = new AsymKeyItem(deviceKeyPair);
            return res;
        }

        public static AsymKeyItem LoadDeviceKeyItem()
        {
            var string64 = SecureStorageGet("DeviceKeyItem");
            if (string.IsNullOrWhiteSpace(string64)) return null;
            
            var keyPair = CryptoKeyPair.Create(string64);
            if (keyPair?.PublicKey?.KeyAlgo != KeyAlgo.HybridEcMlKem)
            {
                Debug.WriteLine("Ignoring non-PQC DeviceKeyItem");
                return null;
            }

            return new AsymKeyItem(keyPair);
        }

        public static void SaveDeviceKeyItem(AsymKeyItem keyItem)
        {
            SecureStorageSet("DeviceKeyItem", keyItem.KeyPair?.GetBase64String());
        }

        private const string KnownServersKey = "KnownTunnelServers";

        public static List<string> LoadKnownServers()
        {
            var json = SecureStorageGet(KnownServersKey);
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(); }
            catch { return new List<string>(); }
        }

        public static void SaveKnownServers(List<string> servers)
        {
            SecureStorageSet(KnownServersKey, JsonSerializer.Serialize(servers));
        }

        public static void AddKnownServer(string serverUrl)
        {
            if (string.IsNullOrWhiteSpace(serverUrl)) return;
            var servers = LoadKnownServers();
            if (!servers.Contains(serverUrl))
            {
                servers.Add(serverUrl);
                SaveKnownServers(servers);
            }
        }

        public static void ClearKnownServers()
        {
            SaveKnownServers(new List<string>());
        }

    }
}
