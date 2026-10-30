using System;
using System.Collections.Generic;
using System.Text;

namespace Qrs.Tunnel.Client
{
    public class TunnelMessageType
    {
        // SwitchPubKey-Requested, SwitchPubKey-Rejected,  Pairing-Requested, Pairing-Accepted
        public static string PAYLOAD = "Payload";
        public static string ASYM_ENCRYPTED = "AEBKID"; // Asym Encrypted Bytes with Key ID
        public static string SWITCH_PUBKEY_REQUESTED = "SwitchPubKey-Requested";
        public static string SWITCH_PUBKEY_REJECTED = "SwitchPubKey-Rejected";
        public static string PAIRING_REQUESTED_MA = "Pairing-Requested-MA";
        public static string PAIRING_REQUESTED_CA = "Pairing-Requested-CA";
        public static string PAIRING_ACCEPTED = "Pairing-Accepted";
        
        //public static string SWITCH_PUBKEY_REQUESTED = "SwitchPubKey-Requested";
        //public static string SWITCH_PUBKEY_REQUESTED = "SwitchPubKey-Requested";
    }
}
