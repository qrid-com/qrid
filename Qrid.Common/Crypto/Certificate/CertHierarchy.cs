//using Utils = Qrid.MobileCrypto.Utils;
using Qrid.Common.Proto.Const;
using System.Collections.Generic;
using KeyRole = Qrid.Common.Proto.Const.KeyRole;

namespace Qrid.Common.Crypto.Certificate
{
    // TODO - add Server2Server data
    public class CertHierarchy
    {
        // must be thread-safe b/c we use singleton here
        // TODO - use ConcurrentDictionary instead of just Dictionary?
        public static CertHierarchy Instance = new CertHierarchy();

        // valid combinations of Issuer's Key Roles and Subject Types

        private Dictionary<string, List<string>> IssuerKeyRoles; // Key - current KeyRole, Value - allowed Issuer's KeyRoles
        private Dictionary<string, List<string>> SubjectTypes; // Key - KeyRole, Value - allowed list of SubjectTypes

        public CertHierarchy()
        {
            InitIssuerKeyRoles();
            InitSubjectTypes();
        }

        public bool IsValidIssuerRole(string myRole, string issuerRole)
        {
            if (IssuerKeyRoles.TryGetValue(myRole, out var list2))
                // no need to lock(list2) b/c we do not have code that modifies these Dictionary or Lists
                foreach (var item in list2)
                    if (item == issuerRole)
                        // issuerRole is valid for myRole
                        return true;

            return false;
        }

        public bool IsValidSubjectType(string myRole, string mySubjectType)
        {
            if (SubjectTypes.TryGetValue(myRole, out var list2))
                // no need to lock(list2) b/c we do not have code that modifies these Dictionary or Lists
                foreach (var item in list2)
                    if (item == mySubjectType)
                        // mySubjectType is valid for myRole
                        return true;

            return false;
        }

        private void InitIssuerKeyRoles()
        {
            // Key - current KeyRole, Value - allowed Issuer's KeyRoles
            IssuerKeyRoles = new Dictionary<string, List<string>>(16);

            // CA - Issuer is upper level CA
            IssuerKeyRoles.Add(KeyRole.CA.Reputation, new List<string> { KeyRole.CA.RootOfTrust });
            IssuerKeyRoles.Add(KeyRole.CA.RepServiceConfig, new List<string> { KeyRole.CA.Reputation });
            IssuerKeyRoles.Add(KeyRole.CA.Public, new List<string> { KeyRole.CA.RootOfTrust });
            IssuerKeyRoles.Add(KeyRole.CA.Domain, new List<string> { KeyRole.CA.Public });
            IssuerKeyRoles.Add(KeyRole.CA.DomainConfig, new List<string> { KeyRole.CA.Domain });
            IssuerKeyRoles.Add(KeyRole.CA.DomainAccounts, new List<string> { KeyRole.CA.Domain });
            IssuerKeyRoles.Add(KeyRole.CA.User, new List<string> { KeyRole.CA.DomainAccounts });
            IssuerKeyRoles.Add(KeyRole.CA.AppUser, new List<string> { KeyRole.CA.User });  // CA.AppUser is not being used at the moment

            // Mobile App
            IssuerKeyRoles.Add(KeyRole.MobileApp.ApiAccess, new List<string> { KeyRole.CA.User });
            IssuerKeyRoles.Add(KeyRole.MobileApp.TunnelReceiver, new List<string> { KeyRole.CA.User });
            IssuerKeyRoles.Add(KeyRole.MobileApp.AsymKeyWrap, new List<string> { KeyRole.CA.User });

            // Client App - Issuer is UserCA for now (to be changed to AppUserCA)
            IssuerKeyRoles.Add(KeyRole.ClientApp.ApiAccess, new List<string> { KeyRole.CA.User });
            IssuerKeyRoles.Add(KeyRole.ClientApp.MessageRead, new List<string> { KeyRole.CA.User });
            IssuerKeyRoles.Add(KeyRole.ClientApp.MessageSign, new List<string> { KeyRole.CA.User });
            IssuerKeyRoles.Add(KeyRole.ClientApp.DataSign, new List<string> { KeyRole.CA.User });

            // Server - Issuer is DomainConfigCA
            IssuerKeyRoles.Add(KeyRole.Server.MobileApi, new List<string> { KeyRole.CA.DomainConfig });
            IssuerKeyRoles.Add(KeyRole.Server.MessagingApi, new List<string> { KeyRole.CA.DomainConfig });
            IssuerKeyRoles.Add(KeyRole.Server.KeyApi, new List<string> { KeyRole.CA.DomainConfig });
            IssuerKeyRoles.Add(KeyRole.Server.StorageApi, new List<string> { KeyRole.CA.DomainConfig });
            IssuerKeyRoles.Add(KeyRole.Server.EnclaveRead, new List<string> { KeyRole.CA.DomainConfig });
            IssuerKeyRoles.Add(KeyRole.Server.EmailerRead, new List<string> { KeyRole.CA.DomainConfig });

            // Server2Server - Issuer is DomainConfigCA
            IssuerKeyRoles.Add(KeyRole.Server2Server.MessageRead, new List<string> { KeyRole.CA.DomainConfig });
            IssuerKeyRoles.Add(KeyRole.Server2Server.MessageSign, new List<string> { KeyRole.CA.DomainConfig });

            // Reputation
            IssuerKeyRoles.Add(KeyRole.ReputationToken.Sign, new List<string> { KeyRole.CA.Reputation });
            IssuerKeyRoles.Add(KeyRole.ReputationToken.Request, new List<string> { KeyRole.CA.User });
            
        }
        
        private void InitSubjectTypes()
        {
            // Key - KeyRole, Value - allowed list of SubjectTypes
            SubjectTypes = new Dictionary<string, List<string>>(16);

            // CA - 
            SubjectTypes.Add(KeyRole.CA.Reputation, new List<string> { SubjectType.CertificateAuthority });
            SubjectTypes.Add(KeyRole.CA.RepServiceConfig, new List<string> { SubjectType.Domain });
            SubjectTypes.Add(KeyRole.CA.Public, new List<string> { SubjectType.CertificateAuthority });
            SubjectTypes.Add(KeyRole.CA.Domain, new List<string> { SubjectType.Domain });
            SubjectTypes.Add(KeyRole.CA.DomainConfig, new List<string> { SubjectType.Domain });
            SubjectTypes.Add(KeyRole.CA.DomainAccounts, new List<string> { SubjectType.Domain });
            SubjectTypes.Add(KeyRole.CA.User, new List<string> { SubjectType.User });
            SubjectTypes.Add(KeyRole.CA.AppUser, new List<string> { SubjectType.App });

            // Mobile App - SubjectType.User
            SubjectTypes.Add(KeyRole.MobileApp.ApiAccess, new List<string> { SubjectType.User });
            SubjectTypes.Add(KeyRole.MobileApp.TunnelReceiver, new List<string> { SubjectType.User });
            SubjectTypes.Add(KeyRole.MobileApp.AsymKeyWrap, new List<string> { SubjectType.App });

            // Client App - SubjectType.App
            SubjectTypes.Add(KeyRole.ClientApp.ApiAccess, new List<string> { SubjectType.App });
            SubjectTypes.Add(KeyRole.ClientApp.MessageRead, new List<string> { SubjectType.App });
            SubjectTypes.Add(KeyRole.ClientApp.MessageSign, new List<string> { SubjectType.App });
            SubjectTypes.Add(KeyRole.ClientApp.DataSign, new List<string> { SubjectType.App });
            SubjectTypes.Add(KeyRole.ReputationToken.Request, new List<string> { SubjectType.App });

            // Server - SubjectType.Server
            SubjectTypes.Add(KeyRole.Server.MobileApi, new List<string> { SubjectType.Server });
            SubjectTypes.Add(KeyRole.Server.MessagingApi, new List<string> { SubjectType.Server });
            SubjectTypes.Add(KeyRole.Server.KeyApi, new List<string> { SubjectType.Server });
            SubjectTypes.Add(KeyRole.Server.StorageApi, new List<string> { SubjectType.Server });
            SubjectTypes.Add(KeyRole.Server.EnclaveRead, new List<string> { SubjectType.Server });
            SubjectTypes.Add(KeyRole.Server.EmailerRead, new List<string> { SubjectType.Server });

            // Server2Server - SubjectType.Server
            SubjectTypes.Add(KeyRole.Server2Server.MessageRead, new List<string> { SubjectType.Server });
            SubjectTypes.Add(KeyRole.Server2Server.MessageSign, new List<string> { SubjectType.Server });
        }

        public string ToShortStr()
        {
            return "";
            //return $"UserKey={UserKey.ToShortStr()}, KeyObject?.DataBytes='{KeyObject?.DataBytes.ToBase64()}'";
        }

    }
}
