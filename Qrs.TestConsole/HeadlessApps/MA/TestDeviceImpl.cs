using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Qrs.TestConsole.HeadlessApps.MA
{
    public class TestDeviceImpl : Mobile.Client.Device.IDevice
    {
        private string dataFile;
        private Dictionary<string, string> data;

        public TestDeviceImpl(string instanceName = "default")
        {
            var userRootPath = Path.Combine(Config.RootDir, instanceName);
            Directory.CreateDirectory(userRootPath);
            dataFile = Path.Combine(userRootPath, "QrsTestDeviceData_" + instanceName + ".json");
            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(dataFile))
            {
                var json = File.ReadAllText(dataFile);
                data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            }
            else
            {
                data = new Dictionary<string, string>();
            }
        }

        private void SaveData()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(data);
            File.WriteAllText(dataFile, json);
        }

        #region Interface Impl
        public void SecureStorageClear()
        {
            data = new Dictionary<string, string>();
            SaveData();
        }

        public string SecureStorageGet(string key)
        {
            // Xamarin's SecureStorage:
            // return Decrypted string or null if key does not exist.

            // so if key does not exist, return null
            return data.TryGetValue(key, out string value) ? value : null;
        }

        public void SecureStorageSet(string key, string value)
        {
            data[key]=value;
            SaveData();
        }

        #endregion Interface Impl
    }
}
