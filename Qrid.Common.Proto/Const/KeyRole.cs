namespace Qrid.Common.Proto.Const
{
    public class KeyRole
    {
        // strings should be marked with "." at the end 
        // TODO - consider using shorter values for key roles, to reduce the certificate size

        public class Server2Server
        {
            public const string MessageRead = "s2s.message_read.";
            public const string MessageSign = "s2s.message_sign.";
        }

        public class Server
        {
            public const string MobileApi = "server.mobile_api.";
            public const string MessagingApi = "server.messaging_api.";
            public const string KeyApi = "server.key_api.";
            public const string StorageApi = "server.storage_api.";
            public const string ReputationApi = "server.reputation_api.";
            public const string EnclaveRead = "server.enclave_read.";
            public const string EmailerRead = "server.emailer_read.";
            public const string EnclaveProvision = "server.enclave_provision.";
        }

        // these are not really the KeyRoles used in Certificates, these are just KeyNames used for Symmetric Keys in both QRID MobileApp(MA) and Client Applications(CA)
        public class SharedKeyName
        {
            public const string ContactMasterKey = "ContactMasterKey";
            public const string ContactHmacKey = "ContactHmacKey";
            public const string BlueKeyRoot = "BlueKeyRoot";
        }

        public class MobileApp
        {
            public const string ApiAccess = "mobile_app.api_access.";
            public const string TunnelReceiver = "mobile_app.tunnel_receiver.";
            public const string AsymKeyWrap = "mobile_app.asym_key_wrap.";
        }

        public class ClientApp
        {
            public const string ApiAccess = "client_app.api_access.";
            public const string MessageRead = "client_app.message_read.";
            public const string MessageSign = "client_app.message_sign.";
            public const string DataSign = "client_app.data_sign.";
        }

        public class CA // CertificateAuthority
        {
            public const string RootOfTrust = "ca.root.";
            public const string Reputation = "ca.reputation.";
            public const string RepServiceConfig = "ca.rep_service_config."; // for signing RepServiceConfig 
            public const string Public = "ca.public.";
            public const string Domain = "ca.domain.";
            public const string DomainConfig = "ca.domain_config."; // for signing Domain Config and Domain Servers
            public const string DomainAccounts = "ca.domain_accounts."; // for signing Domain Users
            public const string User = "ca.user.";
            public const string AppUser = "ca.app_user."; // NOT USED - not used in any real cert or app. Only referenced in code that validates cert hierarhy, as one of the allowed cert tree branches

        }

        // ReputationToken class - hybrid - Sign goes into KeyRole, Contact|Complain go into CertifiedRecipient.CertificateType
        public class ReputationToken
        {
            public const string Contact = "rt.contact.";
            public const string Complain = "rt.complain.";
            public const string Sign = "rt.sign."; // for signing CertifiedRecipient in CertifiedRecipientToken 
            public const string Request = "rt.request."; // for User to sign requests for tokens. Signed requests are then passed to Rep Server for issuing tokens
        }
    }
}
