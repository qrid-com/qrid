namespace Qrid.Common.Proto.Const
{
    public class KeyRolePath
    {
        public const string Separator = "/";

        // Reputation
        public const string ReputationTokenSigner = Separator + KeyRole.CA.RootOfTrust + Separator + KeyRole.CA.Reputation + Separator + KeyRole.ReputationToken.Sign;
        public const string RepServiceConfigSigner = Separator + KeyRole.CA.RootOfTrust + Separator + KeyRole.CA.Reputation + Separator + KeyRole.CA.RepServiceConfig;

        // Domain Accounts
        public const string DomainAccounts = Separator + KeyRole.CA.RootOfTrust + Separator + KeyRole.CA.Public + Separator + KeyRole.CA.Domain + Separator + KeyRole.CA.DomainAccounts;

        // Server
        public const string DomainConfig = Separator + KeyRole.CA.RootOfTrust + Separator + KeyRole.CA.Public + Separator + KeyRole.CA.Domain + Separator + KeyRole.CA.DomainConfig;
        public const string EnclaveRead = DomainConfig + Separator + KeyRole.Server.EnclaveRead;
        public const string EmailerRead = DomainConfig + Separator + KeyRole.Server.EmailerRead;
        public const string EnclaveProvision = DomainConfig + Separator + KeyRole.Server.EnclaveProvision;

        // MA
        public const string AccountCertificate = Separator + KeyRole.CA.RootOfTrust + Separator + KeyRole.CA.Public + Separator + KeyRole.CA.Domain + Separator + KeyRole.CA.DomainAccounts + Separator + KeyRole.CA.User;
        public const string MobileTunnelReceiver = AccountCertificate + Separator + KeyRole.MobileApp.TunnelReceiver;
        public const string MobileApiAccess = AccountCertificate + Separator + KeyRole.MobileApp.ApiAccess;
        public const string MobileAsymKeyWrap = AccountCertificate + Separator + KeyRole.MobileApp.AsymKeyWrap;

        // CA
        public const string ClientApiAccess = AccountCertificate + Separator + KeyRole.ClientApp.ApiAccess;
        public const string ClientDataSign = AccountCertificate + Separator + KeyRole.ClientApp.DataSign;
        public const string ClientMessageSign = AccountCertificate + Separator + KeyRole.ClientApp.MessageSign;
        public const string ClientMessageRead = AccountCertificate + Separator + KeyRole.ClientApp.MessageRead;
    }
}
