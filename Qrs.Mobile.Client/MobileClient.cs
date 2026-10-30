using Qrs.Client.Api;

namespace Qrs.Mobile.Client
{
    public class MobileClient : QrsApiClient
    {

        public override string DefaultPort()
        {
            return "5001";
        }

    }
}
