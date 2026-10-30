using Qrid.Common.Proto.Crypto;
using System;
//using Utils = Qrid.MobileCrypto.Utils;
using GPB = Google.Protobuf;

namespace Qrid.Common.Crypto.Certificate
{

    public class Subject
    {
        public string Name { get; set; }
        public string Type { get; set; }

    }
    public class CertCacheItem
    {
        public GPB.ByteString CertificateData { get; set; }
        public CertifiedPublicKey CertifiedPublicKey { get; set; } // trusted cert info that is signed by issuer

        public SubjectsList Subjects { get; set; } // subjects info from this cert and up to the root
        public string KeyRolePath { get; set; } // concatenated KeyRoles from root to this cert
        
        public bool IsValid { get; set; }   // is this Certificate Valid or not
        public DateTime LastChecked { get; set; } // last time we have checked the Validity of this Certificate

        public int Key32 { get; set; }

        public CertCacheItem()
        {
        }

        public bool Validate()
        {
            return true;
        }

        public string ToShortStr()
        {
            return "";
            //return $"UserKey={UserKey.ToShortStr()}, KeyObject?.DataBytes='{KeyObject?.DataBytes.ToBase64()}'";
        }

    }
}
