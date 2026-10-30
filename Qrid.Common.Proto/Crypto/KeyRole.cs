namespace Qrid.Common.Proto.Crypto
{
    public class KeyRole_Obsolete
    {

        public class MobileApp
        {
            public const string ACCOUNT_ASYM = "ACCOUNT_ASYM";
            public const string KEY_SERVER_ACCESS = "KEY_SERVER_ACCESS";
        }

        public class ClientApp
        {
            public const string ALL_API_ACCESS = "ALL_API_ACCESS";
            public const string KEY_API_ACCESS = "KEY_API_ACCESS";
        }

        public class CertificateAuthority
        {
            public const string HSM_ASYM = "HSM_ASYM";
        }




    }
}
