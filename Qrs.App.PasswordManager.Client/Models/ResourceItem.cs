using System;

namespace Qrs.App.PasswordManager.Client.Models
{
    public enum MailItemStatus
    {
        Encrypted,
        Decrypted
    }
    public class ResourceItem
    {
        
        public ResourceInfo ResourceInfo; // data
        public SearchedItem SearchResultItem; // contains regex matches to highlight in subj and body

        public string ErrorMessage; // could be shown to user

        public void PrepareObjects()
        {
            var rin = new ResourceInfo();
            rin.PrepareObjects();  // new ResourceData and Secret
            rin.SecretWrapper.CreatePayload(); // new Payload (former "Secret")
            ResourceInfo = rin;
        }

        public string CreatedString()
        {
            var d = ResourceInfo?.ResourceData?.Created?.ToDateTime();
            if (d.HasValue)
            {
                var n = DateTime.Now;

                if (d.Value.Date == n.Date)
                {
                    // Today
                    return d.Value.ToString("h:mm tt");
                }
                if (d.Value.Date.AddDays((double)d.Value.DayOfWeek) == n.Date.AddDays((double)n.DayOfWeek))
                {
                    // This Week
                    return d.Value.ToString("ddd h:mm tt");
                }
                return d.Value.ToString("MM/dd/yyyy");
            }
            return string.Empty;
        }
        public string GetError()
        {
            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                return ErrorMessage;
            if (ResourceInfo == null)
                return "ResourceInfo object is null";

            return string.Empty;
        }

    }
}
