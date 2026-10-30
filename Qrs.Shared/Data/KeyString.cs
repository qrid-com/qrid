using Qrid.Common.Data;

namespace Qrs.Shared.Data
{
    public class KeyString : QrKeyValueBase
    {
        public const string KEY_TYPE_QRS = "qrs";
        
        public static KeyString Create(string qr)
        {
            var res = new KeyString();
            res.Parse(qr);
            return res;
        }

        public KeyString():base()
        {
        }

        public string KeyType
        {
            get { return GetParam("keytype"); }
            set { SetParam("keytype", value); }
        }
        public string WrappedKey
        {
            get { return GetParam("wrappedkey"); }
            set { SetParam("wrappedkey", value); }
        }
        
        
        public bool IsValidCmd => IsValid ;

    }
}
