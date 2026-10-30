using Qrid.Common.Client.Api;
using Qrid.Common.Client.Cookie;
using Qrid.Common.Crypto;
using Qrid.Common.Http;
using Qrid.Common.Proto.Crypto;
using Qrid.Common.Utils;
using Qrs.Discovery.Client;
using Qrs.Proto;
using Qrs.Proto.Api;
using Qrs.Proto.Discovery;
using Qrs.Tunnel.Client;
using Qrs.Tunnel.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using BC = Qrs.Tunnel.Client;
using QCP = Qrid.Common.Proto;

namespace Qrs.Client.Context
{

    public enum QrCommandStatus
    {
        None,
        ServerSessionStarted,
        Error,
        Success
    }

    public enum Validation
    {
        None,
        Resealing,
        Full
    }

    public partial class ClientContext : CommonContext
    {

        public readonly string AppName;

        private int TimeoutMs = 120000;

        public QrCommandStatus QrCommandStatus { get; set; }

        public int CommandCount = 0; // number of successfully completed commands via ExecuteQrCommand method
        public bool CanPair => CommandCount == 0; // only allow Pairing the new key when no successful commands have been completed yet
        public bool CanSwitchPubKey => CommandCount == 0; // only allow Pairing the new key when no successful commands have been completed yet

        public string Domain { get; set; }
        public QrsConfiguration Config; // Domain Config

        // Optional API key that a Client Application (CA) presents to the tunnel server to
        // authorize creation of new sessions. Sourced from user settings (sibling to
        // TunnelServiceUri) and/or domain discovery; only CAs need it - the Mobile App never
        // creates a session. Sent to the server as a cookie by the TunnelClient.
        public string ApiKey { get; set; }

        public Qrid.Common.Client.UI.IUserInteraction UI { get; set; }
        public IAsymKeyItemProvider AsymKeyItemProvider { get; set; }
        public ICookieProvider CookieProvider { get; set; }

        public BC.TunnelClient TunnelClient { get; set; }
        


        #region Constructors

        public ClientContext(string appName, Qrid.Common.Client.UI.IUserInteraction ui, IAsymKeyItemProvider asymKeyItemProvider, ICookieProvider cookieProvider) 
        {
            AppName = appName;
            UI = ui;
            AsymKeyItemProvider = asymKeyItemProvider;
            CookieProvider = cookieProvider;
        }

        #endregion

        public async Task<HttpResponseModel<StringResponse>> InitDomainAsync(string domain)
        {
            var discovered = await DiscoveryClient.Instance.GetDomainQrsConfigurationAsync(domain);
            if (!discovered.IsSuccess()) { return ErrorResponse<StringResponse>(discovered); }

            var res = InitDomain(discovered.Data.QrsConfiguration);
            return res ? SuccessStringResponse(null) : ErrorStringResponse($"Domain '{domain}' supports QRS protocols but QRS client initialization has failed");
        }

        public bool InitDomain(QrsConfiguration qrsConfig)
        {
            Reset();
            Config = qrsConfig;
            Domain = Config.Domain; // ???

            TunnelClient = new BC.TunnelClient(Config.TunnelServiceUri, AppName, Directions.CA2MA, CookieProvider);
            // user-provided ApiKey (from settings) wins; fall back to a discovery-provided key.
            TunnelClient.ApiKey = !string.IsNullOrWhiteSpace(ApiKey) ? ApiKey : qrsConfig.ApiKey;
            UI.SetTunnelServiceUri(Config.TunnelServiceUri);
            return true;
        }

        public HttpResponseModel<StringResponse> InitDomain(string tunnelServiceUriTemplate)
        {
            Reset();
            Config = null;
            Domain = null; // ???

            var tunnelServiceUri = tunnelServiceUriTemplate;
            if (tunnelServiceUri.Contains("%localip%"))
            {
                var localip = NetworkUtils.GetLocalIPAddress()?.ToString();
                if (string.IsNullOrWhiteSpace(localip))
                    return ErrorStringResponse($"Cannot determine local IP address which is needed to calculate TunnelServiceUri based on the template '{tunnelServiceUriTemplate}'");
                
                tunnelServiceUri = tunnelServiceUri.Replace("%localip%", localip);
            }

            TunnelClient = new BC.TunnelClient(tunnelServiceUri, AppName, Directions.CA2MA, CookieProvider);
            TunnelClient.ApiKey = ApiKey;
            return SuccessStringResponse();
        }

        public async Task<HttpResponseModel<StringResponse>> InitFromConfigAsync(string tunnelServiceDomain, string tunnelServiceUriTemplate, string apiKey = null)
        {
            // Optional api key required by some tunnel servers to create new sessions.
            ApiKey = apiKey;

            // 1) TRY DOMAIN config param
            if (!string.IsNullOrWhiteSpace(tunnelServiceDomain))
            {
                // DOMAIN - init via domain discovery
                var discoveryResponse = await DiscoveryClient.Instance.GetDomainQrsConfigurationAsync(tunnelServiceDomain);
                if (discoveryResponse.IsSuccess())
                {
                    // discovery succeeded, continue with init
                    var x = InitDomain(discoveryResponse.Data.QrsConfiguration);
                    return x ? SuccessStringResponse() : ErrorStringResponse($"Discovery successful for domain='{tunnelServiceDomain}' but then Init failed - should never happen");
                }
                else
                {
                    // domain discovery failed
                    return ErrorResponse<StringResponse>(discoveryResponse);
                }
            }

            // 2) TRY URI TEMPLATE config param
            if (!string.IsNullOrWhiteSpace(tunnelServiceUriTemplate))
            {
                // URI TEMPLATE - init via template
                var initDomainResponse = InitDomain(tunnelServiceUriTemplate);
                if (initDomainResponse.IsSuccess())
                    return SuccessStringResponse();
                else
                    return ErrorResponse<StringResponse>(initDomainResponse);
            }

            // 3) NO CONFIG PARAMS - just use domain QRID.COM
            tunnelServiceDomain = "qrid.com";
            if (!string.IsNullOrWhiteSpace(tunnelServiceDomain))
            {
                // DOMAIN - init via domain discovery
                var discoveryResponse = await DiscoveryClient.Instance.GetDomainQrsConfigurationAsync(tunnelServiceDomain);
                if (discoveryResponse.IsSuccess())
                {
                    // discovery succeeded, continue with init
                    var x = InitDomain(discoveryResponse.Data.QrsConfiguration);
                    return x ? SuccessStringResponse() : ErrorStringResponse($"Discovery successful for domain='{tunnelServiceDomain}' but then Init failed - should never happen");
                }
                else
                {
                    // domain discovery failed
                    return ErrorResponse<StringResponse>(discoveryResponse);
                }
            }

            return ErrorResponse<StringResponse>("code cannot reach here");
        }


        public void Reset()
        {
            Domain = null;
            Config = null;
            TunnelClient = null;
        }

        [Conditional("DEBUG")]
        public void LogDebug(string message)
        {
            Console.WriteLine($"{DateTime.Now.ToString()} - {message}");
            Debug.WriteLine($"{DateTime.Now.ToString()} - {message}");

        }

        public async Task<HttpResponseModel<QCP.TypedMessage>> ExecuteQrCommandAsync(string qrCommand, QCP.ProtoBase request)
        {
            LogDebug("ExecuteQrCommand - Start");
            QrCommandStatus = QrCommandStatus.None;

            // init random sender keys, reset message numbers
            TunnelClient.InitClientApplication();
            if (CanSwitchPubKey)
                InitClientApplicationWithMostRecentReceiverKey();

            UI.SetTunnelServiceUri(TunnelClient.BaseUrl);

            // prepare QR
            var qrString = TunnelClient.GetQrString(qrCommand);

            // show QR
            LogDebug("Start - TunnelClient.UI.ShowQrCodeWindow");

            await UI.ShowQrCodeWindowAsync("SessionId not used", qrString);
            LogDebug("End   - TunnelClient.UI.ShowQrCodeWindow");

            // main flow
            var response = await MainFlowAsync(request);

            // hide QR
            UI.HideQrCodeWindow(string.Empty);

            return response;
        }

        public async Task<HttpResponseModel<QCP.TypedMessage>> MainFlowAsync(QCP.ProtoBase request)
        {
            // TODO - need to handle QrCommandStatus somewhere ...

            // when running integration tests, this will signal to the waiting Mobile App Context Task that it could continue to "SCAN QR CODE" step,
            // and it will be able to find this Session on the server b/c we have just completed SessionStartAsync() call
            //QrCommandStatus = QrCommandStatus.ServerSessionStarted;

            TimeoutMs = 120000;
            var switchAllowed = true;
            var pairingAllowed = true;

            if (TunnelClient.ReceiverAsym == null)
            {
                // PAIRING - not a single key, ask MA for Pairing
                var pairingRequestCaToMa = QCP.TypedMessage.Create(BC.TunnelMessageType.PAIRING_REQUESTED_CA, new byte[0]);
                var pairingResponse = await PairingFlowAsync(null, pairingRequestCaToMa);
                if (pairingResponse.IsError())
                    return ErrorResponse<QCP.TypedMessage>(pairingResponse);
                // if no error, then ReceiverAsym has been initialized (as the result of Pairing Flow)
            }

        // =========================================================

        fetch_command_payload:

            // PAYLOAD CALL 
            LogDebug("Payload Call...");
            var response1 = await GetPayloadFlowAsync(request, 120000);
            if (response1.IsError())
                return ErrorResponse<QCP.TypedMessage>(response1);

            var mh1 = response1.Data;

            if (mh1.IsPayload)
            {
                // SUCCESS
                CommandCount += 1;
                return SuccessResponse<QCP.TypedMessage>(mh1.TypedMessage);
            }

            if (mh1.IsSwitchRequested)
            {
                LogDebug("Switching of PubKey has been requested by MA (by QRS Mobile App)...");

                if (!CanSwitchPubKey) return LoggedError($"Cannot SWITCH to another PubKey b/c in this session we have already performed {CommandCount} successful operations with the existing PubKey. If you still want to switch, please close and re-open this application to start a new session.");
                if (!switchAllowed) return LoggedError($"Switching Public Key has been requested more than once.");

                // try SWITCH PUB KEY
                switchAllowed = false; // allow only 1 attempt, so block subsequent attempts in this same method 
                var proposedKeyId = mh1.GetSwitchPubKeyRequest();
                var switched = SwitchPubKey(proposedKeyId);
                if (!switched)
                {
                    // PAIRING - switch failed, signal that to MA so MA could send us PAIRING request together with its PubKey
                    if (!pairingAllowed) return LoggedError($"Pairing has been requested more than once.");
                    pairingAllowed = false; // allow only 1 attempt
                    var switchRejectedCaToMa = QCP.TypedMessage.Create(TunnelMessageType.SWITCH_PUBKEY_REJECTED, new byte[0]);
                    var pairingResponse = await PairingFlowAsync(null, switchRejectedCaToMa);
                    if (pairingResponse.IsError())
                        return LoggedError(pairingResponse.Message);
                }

                // either Switch or Pairing has succeeded
                // ready to perform command again, with the newly initialized DevicePubKey

                LogDebug("Pairing or Switching has succeeded. Going to fetch payload");
                goto fetch_command_payload;
            }

            if (mh1.IsError)
                // error and we have error message
                return LoggedError($"{mh1.ErrorMessage}");
            else
                // unexpected error
                return LoggedError($"unexpected error while trying to process command result message");
        }

        public HttpResponseModel<QCP.TypedMessage> LoggedError(string errorMessage)
        {
            LogDebug($"ERROR in CA: {errorMessage}");
            return ErrorResponse<QCP.TypedMessage>(errorMessage);
        }

        public async Task<HttpResponseModel<MsgHelper>> GetPayloadFlowAsync(QCP.ProtoBase request, int timeoutMs)
        {
            // MAIN CALL - Encrypt - Send - Decrypt response
            var response = await TunnelClient.EncryptSendPollDecrypt(request, timeoutMs);

            if (response.IsError())
            {
                //QrCommandStatus = QrCommandStatus.Error;
                return ErrorResponse<MsgHelper>(response);
            }

            return SuccessResponse(response.Data);

        }

        public async Task<HttpResponseModel<QCP.TypedMessage>> PairingFlowAsync(PairingRequest requestFromMobileApp, QCP.TypedMessage messageToMobileApp)
        {
            
            if (!CanPair) 
                return ErrorResponse<QCP.TypedMessage>($"Cannot PAIR with another PubKey b/c in this session we have already performed {CommandCount} successful operations with the existing PubKey. If you still want to pair, then close and re-open this application to start a new session.");

            // (!) this is required b/c MA does not have the PrivKey from that ReceiverAsym (otherwise we would not be in this flow) 
            //     so clear it and, until Paired, messages to MA will be Single-encrypted (with Sym) instead of Double-Encrypted 
            //
            TunnelClient.ReceiverAsym = null;

            var pairingRequest = requestFromMobileApp;

            if (pairingRequest == null)
            {
                // pairing needs to be initiated by CA (by us)
                var request = messageToMobileApp;
                // Encrypt - Send - Decrypt response
                var response = await TunnelClient.EncryptSendPollDecryptTypedMessage(request, TimeoutMs);

                if (response.IsError())
                    return ErrorResponse<QCP.TypedMessage>(response);
                var mh = response.Data;
                if (mh.IsError)
                    return ErrorResponse<QCP.TypedMessage>($"Expected Pairing information from MA but received this error: {mh.ErrorMessage}");
                if (!mh.IsPairingRequestedByMa)
                    return ErrorResponse<QCP.TypedMessage>("Mobile Application declined our PAIRING request");
                pairingRequest = mh.GetPairingRequest();
            }

            LogDebug("ClientContext.PairingFlowAsync - before awaiting UI.GetUserInputAsync('pairing-code')");

            // UI - ask user to enter Pairing Code to compare it with what we've got in pairingRequest
            var userResponse = await UI.GetUserInputAsync("pairing-code");
            LogDebug("ClientContext.PairingFlowAsync - after awaiting UI.GetUserInputAsync('pairing-code')");

            if (userResponse.IsError())
                return ErrorResponse<QCP.TypedMessage>(userResponse);

            var userPairingCode = userResponse.Data.StringValue;
            if (userPairingCode != pairingRequest.PairingCode)
                return ErrorResponse<QCP.TypedMessage>($"Invalid Pairing Code! Your code ('{userPairingCode}') is not matching the code from Mobile Application");

            // pairing code is good, accept and save this Pub Key from pairing request
            TunnelClient.ReceiverAsym = new AsymKeyItem(pairingRequest.PubKey);
            LogDebug($"Saving Paired Key, its KeyHashId = '{TunnelClient.ReceiverAsym.KeyHashId}'");
            AsymKeyItemProvider.SaveAsymKeyItem(TunnelClient.ReceiverAsym);
            AsymKeyItemProvider.SetMostRecentKeyId(TunnelClient.ReceiverAsym.KeyHashId);

            return SuccessResponse<QCP.TypedMessage>(null); // null is ok here
        }

        public void InitClientApplicationWithMostRecentReceiverKey()
        {
            if (!CanSwitchPubKey) throw new ApplicationException("Called InitCaWithMostRecentReceiverKey() but CanSwitchPubKey == false , this should not happen");
            var mostRecentKeyId = AsymKeyItemProvider.GetMostRecentKeyId();
            TunnelClient.ReceiverAsym = string.IsNullOrWhiteSpace(mostRecentKeyId) 
                ? null 
                : AsymKeyItemProvider.Get(mostRecentKeyId);
        }

        public bool SwitchPubKey(string proposedKeyId)
        {
            var proposedKey = AsymKeyItemProvider.Get(proposedKeyId);
            if (proposedKey == null) return false;
            TunnelClient.ReceiverAsym = proposedKey;
            LogDebug($"Switched to PubKey with KeyHashId = '{TunnelClient.ReceiverAsym.KeyHashId}', saving it into '_most-recent-key-id' file");
            AsymKeyItemProvider.SetMostRecentKeyId(TunnelClient.ReceiverAsym.KeyHashId);
            return true;
        }

        public async Task<HttpResponseModel<KeyOpResponse>> KeyOpAsync(KeyOpRequest request)
        {
            LogDebug("KeyOpAsync - Start");

            // send request, show QR code, await response
            var response = await ExecuteQrCommandAsync("keyop", request);

            if (response.IsError())
            {
                // some error
                LogDebug($"ERROR - response.Message = '{response.Message}'");
                return TunnelClient.ErrorResponse<KeyOpResponse>(response.Message);
            }

            // create KeyOpResponse object from QrCommandResponse object

            var res = new HttpResponseModel<KeyOpResponse>();
            res.Data = KeyOpResponse.Create(response.Data.MessageBytes);
            LogDebug($"KeyOpAsync - End - SUCCESS");
            return res;
        }

        public bool CancelQrCodePolling()
        {
            return TunnelClient.CancelRequest();
        }

        #region Qrs Crypto Utils methods

        public string GetNewGuid64()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        public AssociatedDataDek GetAssociatedDataDek(Dictionary<string, string> associatedDataItems, string dekId = "", int topLevelKey = 0)
        {
            var res = new AssociatedDataDek();
            res.AppName = AppName;
            res.KekName = string.Empty; // we dont know the KekName, it will be set inside QRS Mobile app
            res.Created = SharedUtils.GetTimestampNowTruncatedToMinutes();
            res.DekId = dekId;
            res.TopLevelKey = topLevelKey;

            foreach (var item in associatedDataItems)
                res.AssociatedDataItems.Add(new AssociatedDataItem { Key = item.Key, Value = item.Value });

            return res;
        }

        public AssociatedData GetAssociatedDataForPayload(AssociatedDataDek adDek)
        {
            var adp = AssociatedDataPayload.Create(AppName, adDek.DekId);
            var res = AssociatedData.Create(adp);
            return res;
        }

        #endregion Qrs Crypto Utils methods

    }
}
