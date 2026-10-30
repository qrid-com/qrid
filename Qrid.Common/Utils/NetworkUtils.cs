using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace Qrid.Common.Utils
{
    public class NetworkUtils
    {
        public static System.Net.IPAddress GetLocalIPAddress()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet || n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                .Where(n => n.Name == "Wi-Fi")
                .SelectMany(n => n.GetIPProperties()?.UnicastAddresses)
                .Where(n => n.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                .Select(g => g?.Address)
                .Where(a => a != null)
                .FirstOrDefault();
        }


    }
}
