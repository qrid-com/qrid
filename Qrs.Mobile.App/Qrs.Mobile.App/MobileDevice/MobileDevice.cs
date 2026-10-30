using Qrs.Mobile.Client.Device;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Qrs.Mobile.App.MobileDevice
{
    public class MobileDeviceImpl : IDevice
    {
        public MobileDeviceImpl()
        {
        }

        public string SecureStorageGet(string key)
        {
            return Task.Run(async () => await SecureStorage.GetAsync(key).ConfigureAwait(false))
                .GetAwaiter().GetResult();
        }

        public void SecureStorageSet(string key, string value)
        {
            Task.Run(async () => await SecureStorage.SetAsync(key, value ?? string.Empty).ConfigureAwait(false))
                .GetAwaiter().GetResult();
        }

        public async Task<string> SecureStorageGetAsync(string key)
        {
            return await SecureStorage.GetAsync(key);
        }

        public async Task SecureStorageSetAsync(string key, string value)
        {
            await SecureStorage.SetAsync(key, value ?? string.Empty);
        }

        public void SecureStorageClear()
        {
            SecureStorage.RemoveAll();
        }
    }
}
