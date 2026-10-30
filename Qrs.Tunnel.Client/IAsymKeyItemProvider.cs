using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.Tunnel.Client
{
    public interface IAsymKeyItemProvider
    {
        List<AsymKeyItem> GetAllAsymKeyItems();
        AsymKeyItem Get(string keyId);
        void SaveAsymKeyItem(AsymKeyItem item);

        string GetMostRecentKeyId();
        void SetMostRecentKeyId(string keyId);

    }
}
