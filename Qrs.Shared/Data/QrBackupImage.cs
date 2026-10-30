using Qrid.Common.Data;

namespace Qrs.Shared.Data
{
    public class QrBackupImage : QrKeyValueBase
    {
        public static QrBackupImage Create(string qr)
        {
            var res = new QrBackupImage();
            res.Parse(qr);
            return res;
        }

        public QrBackupImage():base()
        {
        }

        // password-encrypted MasterKey bytes in Base64 encoding
        public string Key
        {
            get { return GetParam("key"); }
            set { SetParam("key", value); }
        }

        // Name of MasterKey
        public string Name
        {
            get { return GetParam("name"); }
            set { SetParam("name", value); }
        }

        public bool ContainsName => Params.ContainsKey("name");

        public bool IsValidBackupImage => IsValid && !string.IsNullOrWhiteSpace(Key);
    }
}
