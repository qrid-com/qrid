using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.Mobile.Client.Device
{
    public interface IDevice
    {
        void SecureStorageSet(string key, string value);
        string SecureStorageGet(string key);
        void SecureStorageClear();
    }
}
