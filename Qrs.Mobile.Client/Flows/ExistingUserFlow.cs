using Qrs.Shared.Data;

namespace Qrs.Mobile.Client.Flows
{
    public class ExistingUserFlow
    {
        public string Domain;
        public string Password;

        public string QrCodeString; // todo - remove this, use QBI instead
        public QrBackupImage QrBackupImage;

        public Data.OrangeKeys OrangeKeysCandidate;

    }
}
