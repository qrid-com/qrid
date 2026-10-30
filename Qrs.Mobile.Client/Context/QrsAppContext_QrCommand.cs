using Qrid.Common.Data;
using Qrid.Common.Http;
using Qrid.Common.Proto;
using Qrs.Proto.Api;
using Qrs.Tunnel.Client;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BC = Qrs.Tunnel.Client;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;
using QP = Qrid.Common.Proto;

namespace Qrs.Mobile.Client.Context
{

    public partial class QrsAppContext 
    {

        public async Task DownloadRequestAsync(QrCommand qrCommand)
        {
            QrCommandFlow.Init(qrCommand);
            await PayloadOrPairing();
        }

        public async Task DownloadRequestAfterPairingAsync()
        {

            // Poll then Decrypt
            var response = await TunnelClient.PollDecrypt(30000);
            if (response.IsError())
            {
                QrCommandFlow.ErrorMessage = $"{response.Message}";
                return;
            }

            var mh1 = response.Data;

            if (!mh1.IsPayload)
            {
                QrCommandFlow.ErrorMessage = $"Error - after PAIRING, we have expected PAYLOAD but received '{mh1.TypedMessage.MessageType}'";
                return;
            }

            // success
            QrCommandFlow.Payload = mh1.TypedMessage;
            QrCommandFlow.ParsePayload(OrangeKeys.KeyName);
        }

        public async Task PayloadOrPairing()
        {
            QrCommandFlow.ErrorMessage = string.Empty; // no error for now

            // Validate the Tunnel Server URL from the QR code before using it
            var tunnelUrl = QrCommandFlow.QrCommand?.Server;
            var urlError = BC.TunnelClient.ValidateTunnelUrl(tunnelUrl);
            if (!string.IsNullOrWhiteSpace(urlError))
            {
                QrCommandFlow.ErrorMessage = urlError;
                return;
            }

            // Validate the QRS Tunnel Client Protocol version of the CA from the QR code
            var protocolVersionError = BC.TunnelClient.ValidateQrCommandProtocolVersion(QrCommandFlow.QrCommand);
            if (!string.IsNullOrWhiteSpace(protocolVersionError))
            {
                QrCommandFlow.ErrorMessage = protocolVersionError;
                return;
            }

            // Build security alerts about the Tunnel Server URL
            var knownServers = MobUtils.LoadKnownServers();
            var isQridDomain = Regex.IsMatch(new Uri(tunnelUrl).Host, @"^(\w+\.)?qrid\.com$", RegexOptions.IgnoreCase);
            if (tunnelUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                QrCommandFlow.Alerts.Add($"Tunnel Server '{tunnelUrl}' - using PLAIN HTTP - still acceptable b/c of the 2 additional Application-Level encryption layers provided by QRS application protocol");
            if (!knownServers.Contains(tunnelUrl) && !isQridDomain)
            {
                QrCommandFlow.Alerts.Add($"Tunnel Server '{tunnelUrl}' - new server - using it for the 1st time");
                QrCommandFlow.PendingKnownServer = tunnelUrl;
            }

            // 1. init from QR - set BaseUrl, SessionKey, ClientPubKey. Reset message numbers
            TunnelClient.InitMobileApplication(QrCommandFlow.QrCommand, DeviceKeyItem);

            // 2.2 Poll then Decrypt
            var response = await TunnelClient.PollDecrypt(2000);
            if (response.IsError())
            {
                QrCommandFlow.ErrorMessage = $"{response.Message}";
                return;
            }

            var mh1 = response.Data;

            if (mh1.IsPayload)
            {
                // success - return downloaded payload
                QrCommandFlow.Payload = mh1.TypedMessage;
                return;
            }

            if (mh1.IsKeyIdDifferent)
            {
                // SWITCH PUB KEY to OUR KEY
                // cannot decrypt b/c MA has used Pub Key different from our key
                // ask MA to switch to our key, sending just KeyId (not sending PubKey - that would be Pairing and it has some special flow with user confirming the pairing code)

                LogDebug("MA will send a request to SWITCH PUB KEY");

                var switchPubKeyRequest = TypedMessage.Create(
                    BC.TunnelMessageType.SWITCH_PUBKEY_REQUESTED,
                    StringObject.CreateWithString(DeviceKeyItem.KeyHashId)
                    );

                var switchResponse = await TunnelClient.EncryptSendPollDecryptTypedMessage(switchPubKeyRequest, 60*1000);
                if (switchResponse.IsError())
                {
                    QrCommandFlow.ErrorMessage = $"{switchResponse.Message}";
                    return;
                }

                var mh2 = switchResponse.Data;

                if (mh2.IsPayload)
                {
                    // success - return downloaded payload object
                    QrCommandFlow.Payload = mh2.TypedMessage;
                    return;
                }

                if (!mh2.IsSwitchRejected)
                {
                    QrCommandFlow.ErrorMessage = $"Unexpected - {switchResponse.Message}";
                    return;
                }

                // SWITCH rejected, let's request PAIRING
                // probably CA does not have our Pub Key, so it did not recognize our KeyHashId sent in a switch

                await StartPairing();
                return;
            }

            // CA may have asked for Pairing
            if (mh1.IsPairingRequestedByCa)
            {
                await StartPairing();
                return;
            }

            QrCommandFlow.ErrorMessage = $"Unexpected state - see the source code";

        }


        public async Task StartPairing()
        {
            var pairingCode = CryptoUtils.GetRandomLowercaseLetters(3);
            var pairingRequest = PairingRequest.Create(DeviceKeyItem.PubKey, DeviceKeyItem.KeyHashId, pairingCode);
            var pairingTypedMessage = TypedMessage.Create(TunnelMessageType.PAIRING_REQUESTED_MA, pairingRequest);

            var response = await TunnelClient.EncryptSendTypedMessage(pairingTypedMessage);

            QrCommandFlow.PairingRequest = pairingRequest;

        }

        public async Task<HttpResponseModel<QP.StringObject>> QrCommandSendResponseAsync(QP.ProtoBase response)
        {
            // send message
            var res = await TunnelClient.EncryptSend(response);

            if (res.IsSuccess())
                return SuccessResponse<QP.StringObject>(new QP.StringObject());
            else
                return ErrorResponse<QP.StringObject>($"{res.Message}");
        }


        // this is controlled by checkbox at the Settings pag
        // session only, i.e. it cannot be permanently saved
        public bool SettingsIgnoreRequestValidation = false;

    }
}
