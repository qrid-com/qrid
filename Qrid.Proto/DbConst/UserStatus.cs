namespace Qrid.Proto.DbConst
{
    public class UserStatus
    {
        public const string PENDING = "P";
        public const string INACTIVE = "I"; // rejected by Admins
        public const string ACTIVE = "A";
        public const string WAITING = "W";  // Waiting for AccountCertificate, everything else is ready
    }
}
