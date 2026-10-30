using Qrid.Common.Client.Cookie;
using Qrid.Common.Data;
using Qrid.Common.Http;
using Qrid.Common.Proto;
using Qrid.Common.Proto.Crypto;
using Qrs.Client.Api;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CR = Qrid.Common.Proto.Crypto;
using CryptoUtils = Qrid.Common.Crypto.CryptoUtils;
using Directions = Qrs.Tunnel.Shared.Models.Directions;
using Model = Qrs.Tunnel.Shared.Models;
using QrsTunnelProtocol = Qrs.Tunnel.Shared.QrsTunnelProtocol;
using QCP = Qrid.Common.Proto;

namespace Qrs.Tunnel.Client
{

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(Model.MessageSendPollRequest))]
    [JsonSerializable(typeof(Model.MessageSendPollResponse))]
    [JsonSerializable(typeof(ResponseModelBase))]
    [JsonSerializable(typeof(HttpResponseModel<Model.MessageSendPollResponse>))]
    public partial class TunnelClientJsonContext : JsonSerializerContext
    {
        // HttpResponseModel<T>
    }

    public partial class TunnelClient : QrsApiClient
    {
        // contains code to be re-used in both MA and CA (Mobile Application or Client Application)
        // 

        /// <summary>
        /// QRS Tunnel Client Protocol version (SemVer MAJOR.MINOR.PATCH) - end-to-end protocol between CA and MA.
        /// Manually incremented by QRS developers following Semantic Versioning rules (MAJOR - breaking changes only).
        /// Independent from the QRS Tunnel Server Protocol version (see QrsTunnelProtocol.ServerProtocolVersion).
        /// CA sends it in the QR code; both CA and MA also send it as MessageProtocolVersion with every tunnel message.
        /// MessageProtocolVersion is bound to the Message via SessionKey AEAD, so the Tunnel Server cannot modify it.
        /// </summary>
        public const string ClientProtocolVersion = "1.0.0";

        /// <summary>
        /// Checks if QRS Tunnel Client Protocol version received from the other side (QR code or tunnel message) is supported.
        /// Returns null when supported; returns "please upgrade" error message otherwise.
        /// </summary>
        public static string ValidateClientProtocolVersion(string receivedVersion)
            => QrsTunnelProtocol.ValidateVersion(QrsTunnelProtocol.ClientProtocolName, receivedVersion, ClientProtocolVersion, required: true);

        /// <summary>
        /// Checks if QRS Tunnel Server Protocol version received from Tunnel Server is supported.
        /// Returns null when supported; returns "please upgrade" error message otherwise.
        /// </summary>
        public static string ValidateServerProtocolVersion(string receivedVersion)
            => QrsTunnelProtocol.ValidateServerProtocolVersion(receivedVersion);

        private HttpClient _pollingClient;  // longer timeout for Polling calls
        public string Direction { get; }   // "MA2CA" or "CA2MA" ("SENDER 2 RECEIVER")

        // direction of the messages we receive - it is always the opposite of our own (sending) Direction
        public string ReceivingDirection => Direction == Directions.MA2CA ? Directions.CA2MA : Directions.MA2CA;

        public CR.CryptoKey SessionKey { get; set; }
        public AsymKeyItem SenderAsym { get; set; } // this is "our" key so we have a complete KeyPair, i.e Pub and Priv keys
        public AsymKeyItem ReceiverAsym { get; set; } // this is the other side, we only have their Pub key
        public string SenderPubKey => SenderAsym.PubKeyString;
        public byte[] SenderPubKeyBytes { get; set; }
        public string TunnelHashId { get; set; } // string identifying the Tunnel Session, (contains Hash(SenderPubKeyBytes), but this field is used as string only)

        public JsonSerializerOptions JsonOptions { get; set; }


        // need this to be TRUE only once when MA shows Pairing Page and Continue button on Pairing Page is pressed
        // b/c CA does NOT reset MessageNumber when Pairing, we do not want reset in MA either
        // Q: - why dont we just reset MessageNumbers in CA (after Pairing)? Then we do not need to have this bool variable here
        // A: - in a Rest Controller, we dont have a way to reset MessageNumbers for a given TunnelId. And we definitely do not want to scan another QR code after pairing for a new TunnelId
        public bool DoNotResetMessageNumbers;
        public int MessageNumber { get; set; }
        public int ExpectedResponseNumber { get; set; } // Response.MessageNumber must be >= ExpectedResponseNumber
        public TunnelClient(string direction, string appName, ICookieProvider cookieProvider) : base(null, appName, cookieProvider)
        {
            Direction = direction ?? Directions.CA2MA;
            InitPollingClient();
            InitJsonOptions();
        }

        public TunnelClient(string baseUrl, string appName, string direction, ICookieProvider cookieProvider) : 
            base(baseUrl, appName, cookieProvider)
        {
            Direction = direction ?? Directions.CA2MA;
            InitPollingClient();
            InitJsonOptions();
        }

        private void InitJsonOptions()
        {
            // IMPORTANT - we need case-insensitive property name matching b/c server-side renders JSON responses with camelCase for whatever reason
            JsonOptions = new JsonSerializerOptions { TypeInfoResolver = TunnelClientJsonContext.Default, PropertyNameCaseInsensitive = true };
        }

        private void InitPollingClient()
        {
            // 200 sec slightly over 3 min, server-side timeout should be 3 min we always want server-side expire first
            _pollingClient = Qrid.Common.Client.Api.CommonApiClient.CreateCrossPlatformHttpClient(5, 200);
        }

        public override string DefaultPort()
        {
            return "5000";
        }

        public void InitMobileApplication(QrCommand qc, AsymKeyItem senderKeyItem)
        {
            // init MA at the beginning of every Command
            SenderAsym = senderKeyItem; // with Private Key!
            InitFromQrCommand(qc);
            if (DoNotResetMessageNumbers)
            {
                LogDebug($"{Direction} - Do not reset MessageNumbers");
                DoNotResetMessageNumbers = false;
            }
            else
            {
                LogDebug($"{Direction} - Reset MessageNumbers");
                ResetMessageNumbers();
            }

        }

        public void InitClientApplication()
        {
            // init CA at the beginning of every Command
            InitRandomSenderKeys();
            ResetMessageNumbers();
            ResetPeerMessageProtocolVersion(null);
        }

        private void ResetMessageNumbers()
        {
            LogDebug($"Resetting MessageNumber=0, Direction={Direction} ");
            MessageNumber = 0;
            ExpectedResponseNumber = 1; // 1! not 0!
        }

        private void InitRandomSenderKeys()
        {
            SessionKey = CryptoUtils.GetRandomSymKey();
            SenderAsym = new AsymKeyItem(CryptoUtils.GetRandomAsymCryptoKeyPair(postQuantum: true));
            SenderPubKeyBytes = SenderAsym.KeyPair.PublicKey.GetByteArray();
            TunnelHashId = CryptoUtils.GetTunnelHashId(SenderPubKeyBytes);
        }

        public string GetQrString(string qrCommand)
        {
            var qc = new QrCommand();
            qc.QrVer = "2";
            qc.Cmd = qrCommand;
            qc.Server = BaseUrl;
            qc.ClientPubHash = TunnelHashId;
            qc.SessionKey = SessionKey.GetBase64String();
            qc.ClientProtocolVersion = ClientProtocolVersion;
            return qc.ToQrString();
        }

        /// <summary>
        /// Validates a tunnel server URL from a QR code.
        /// Returns null when valid; returns an error message when invalid.
        /// </summary>
        public static string ValidateTunnelUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "Tunnel Server URL is empty";

            // Reject forbidden characters before any URI parsing
            if (url.Contains('#') || url.Contains('?') || url.Contains('&'))
                return $"Tunnel Server URL '{url}' contains forbidden characters ('?', '&', '#')";

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return $"Tunnel Server URL '{url}' is not a valid URI";

            if (uri.Scheme != "http" && uri.Scheme != "https")
                return $"Tunnel Server URL '{url}' has unsupported scheme '{uri.Scheme}' (only 'http' or 'https' are allowed)";

            if (!string.IsNullOrEmpty(uri.UserInfo))
                return $"Tunnel Server URL '{url}' contains user-info which is not allowed";

            if (!string.IsNullOrEmpty(uri.Fragment))
                return $"Tunnel Server URL '{url}' contains a fragment which is not allowed";

            return null; // valid
        }

        private void InitFromQrCommand(QrCommand qc)
        {
            // url
            BaseUrl = qc.Server;

            // init keys
            SessionKey = CR.CryptoKey.Create(qc.SessionKey);
            // QR v2 keeps only the sender public-key hash in the QR code.
            // The full sender public key is delivered in the first tunnel response and verified against this hash.
            ReceiverAsym = null;
            TunnelHashId = qc.ClientPubHash;
            // CA's protocol version from QR code is trusted (QR code is never seen by Tunnel Server),
            // so pin it right away - all messages from CA in this session must carry the same MessageProtocolVersion
            ResetPeerMessageProtocolVersion(qc.ClientProtocolVersion);
        }

        // Opposite side's QRS Tunnel Client Protocol version (MessageProtocolVersion) pinned for the current session.
        // Remembered from the QR code (MA) or from the 1st successfully decrypted message, and must not change afterwards.
        public string PeerMessageProtocolVersion { get; private set; }
        public bool IsPeerMessageProtocolVersionPinned { get; private set; }

        private void ResetPeerMessageProtocolVersion(string pinnedVersion)
        {
            PeerMessageProtocolVersion = string.IsNullOrWhiteSpace(pinnedVersion) ? null : pinnedVersion;
            IsPeerMessageProtocolVersionPinned = PeerMessageProtocolVersion != null;
        }

        /// <summary>
        /// Validates QRS Tunnel Client Protocol version of the CA delivered in the QR code.
        /// Returns null when supported; returns "please upgrade" error message otherwise.
        /// </summary>
        public static string ValidateQrCommandProtocolVersion(QrCommand qc)
            => ValidateClientProtocolVersion(qc?.ClientProtocolVersion);

        public async Task<HttpResponseModel<MsgHelper>> EncryptSendPollDecrypt(QCP.ProtoBase request, int timeoutMs)
        {
            // reserve the MessageNumber of the outgoing message, it is cryptographically bound to the message (used as AEAD)
            this.MessageNumber++;

            // encrypt
            var msg = DoubleEncryptPayload(request).GetBase64String();

            // send + poll for response
            var resp = await SendPollWithCurrentMessageNumber(msg, timeoutMs);
            if (resp.IsError())
                return ErrorResponse<MsgHelper>(resp);

            var receiverKeyError = EnsureReceiverPublicKeyForQrV2(resp.Data);
            if (!string.IsNullOrWhiteSpace(receiverKeyError))
                return ErrorResponse<MsgHelper>(receiverKeyError);

            // decrypt
            return DecryptIncoming(resp.Data);
        }

        public async Task<HttpResponseModel<MsgHelper>> EncryptSendPollDecryptTypedMessage(QCP.TypedMessage request, int timeoutMs)
        {
            // reserve the MessageNumber of the outgoing message, it is cryptographically bound to the message (used as AEAD)
            this.MessageNumber++;

            // encrypt
            var msg = DoubleEncryptTypedMessage(request).GetBase64String();

            // send + poll for response
            var resp = await SendPollWithCurrentMessageNumber(msg, timeoutMs);
            if (resp.IsError())
                return ErrorResponse<MsgHelper>(resp);

            var receiverKeyError = EnsureReceiverPublicKeyForQrV2(resp.Data);
            if (!string.IsNullOrWhiteSpace(receiverKeyError))
                return ErrorResponse<MsgHelper>(receiverKeyError);

            // decrypt
            return DecryptIncoming(resp.Data);
        }

        public async Task<HttpResponseModel<Model.MessageSendPollResponse>> EncryptSend(QCP.ProtoBase request)
        {
            // reserve the MessageNumber of the outgoing message, it is cryptographically bound to the message (used as AEAD)
            this.MessageNumber++;

            // encrypt
            var msg = DoubleEncryptPayload(request).GetBase64String();

            // send without polling for response ("-1" indicates request.ExpectResponse = false)
            var resp = await SendPollWithCurrentMessageNumber(msg, -1);

            // cannot decrypt b/c we did NOT wait until reply message, 
            // so just return REST call result
            return resp;
        }

        public async Task<HttpResponseModel<Model.MessageSendPollResponse>> EncryptSendTypedMessage(QCP.TypedMessage request)
        {
            // reserve the MessageNumber of the outgoing message, it is cryptographically bound to the message (used as AEAD)
            this.MessageNumber++;

            // encrypt
            var msg = DoubleEncryptTypedMessage(request).GetBase64String();

            // send without polling for response ("-1" indicates request.ExpectResponse = false)
            var resp = await SendPollWithCurrentMessageNumber(msg, -1);

            // cannot decrypt b/c we did NOT wait until reply message, 
            // so just return REST call result
            return resp;
        }

        public async Task<HttpResponseModel<MsgHelper>> PollDecrypt(int timeoutMs)
        {
            // poll for response
            var resp = await MessagePoll(timeoutMs);

            if (resp.IsError())
                return ErrorResponse<MsgHelper>(resp);

            var receiverKeyError = EnsureReceiverPublicKeyForQrV2(resp.Data);
            if (!string.IsNullOrWhiteSpace(receiverKeyError))
                return ErrorResponse<MsgHelper>(receiverKeyError);

            // decrypt
            return DecryptIncoming(resp.Data);
        }

        /// <summary>
        /// Validates the opposite side's MessageProtocolVersion, verifies it has not changed within the session,
        /// then decrypts the Message (MessageProtocolVersion is part of the AEAD, so decryption fails if Tunnel Server has modified it)
        /// and pins MessageProtocolVersion after the 1st successfully decrypted message.
        /// </summary>
        private HttpResponseModel<MsgHelper> DecryptIncoming(Model.MessageSendPollResponse response)
        {
            var messageProtocolVersion = response.MessageProtocolVersion;

            // MessageProtocolVersion is mandatory - it is bound to the Message via AEAD
            var versionError = ValidateClientProtocolVersion(messageProtocolVersion);
            if (!string.IsNullOrWhiteSpace(versionError))
                return ErrorResponse<MsgHelper>(versionError);

            if (IsPeerMessageProtocolVersionPinned && messageProtocolVersion != PeerMessageProtocolVersion)
                return ErrorResponse<MsgHelper>($"{QrsTunnelProtocol.ClientProtocolName} version of the other side has changed within the same session from '{PeerMessageProtocolVersion}' to '{Truncate(messageProtocolVersion)}'");

            MsgHelper mh = DoubleDecrypt(response.Message, response.MessageNumber, messageProtocolVersion);

            // SYM layer has been decrypted, so MessageProtocolVersion has been authenticated via AEAD - remember it for this session
            if (mh.TypedMessageSym != null && !IsPeerMessageProtocolVersionPinned)
            {
                PeerMessageProtocolVersion = messageProtocolVersion;
                IsPeerMessageProtocolVersionPinned = true;
            }

            return SuccessResponse<MsgHelper>(mh);
        }

        private static string Truncate(string value)
            => value.Length > 64 ? value.Substring(0, 64) + "..." : value;

        #region AEAD - Associated Data binding Message to its Direction, MessageNumber and MessageProtocolVersion

        /// <summary>
        /// Associated Data used in all symmetric (SessionKey) tunnel crypto operations.
        /// Only the two clients know the SessionKey, so only they can bind/verify these message attributes.
        /// Binding (Direction + MessageNumber) prevents replaying a tunnel message with another
        /// direction or another message number. Binding MessageProtocolVersion (mandatory) prevents the Tunnel Server
        /// from manipulating the sender's protocol version.
        /// </summary>
        public static byte[] GetTunnelAead(string direction, int messageNumber, string messageProtocolVersion)
        {
            if (string.IsNullOrWhiteSpace(messageProtocolVersion))
                throw new ArgumentException("MessageProtocolVersion is required for tunnel AEAD", nameof(messageProtocolVersion));
            return Encoding.UTF8.GetBytes($"{direction}:{messageNumber}:{messageProtocolVersion}");
        }

        // AEAD for the message we are sending right now - MessageNumber must already be incremented (reserved) for that message
        public byte[] OutgoingAead() => GetTunnelAead(Direction, MessageNumber, ClientProtocolVersion);

        // AEAD for the message we have received - MessageNumber and MessageProtocolVersion come from the incoming message itself
        public byte[] IncomingAead(int messageNumber, string messageProtocolVersion) => GetTunnelAead(ReceivingDirection, messageNumber, messageProtocolVersion);

        #endregion

        #region DoubleEncrypt / Decrypt

        public SymEncryptedBytes DoubleEncryptPayload(byte[] data)
        {
            return DoubleEncryptPayload(data, SessionKey, ReceiverAsym, OutgoingAead());
        }

        public SymEncryptedBytes DoubleEncryptPayload(QCP.ProtoBase data)
        {
            var typedMessage = QCP.TypedMessage.Create(TunnelMessageType.PAYLOAD, data);
            return DoubleEncryptTypedMessage(typedMessage);
        }

        public SymEncryptedBytes DoubleEncryptTypedMessage(QCP.TypedMessage typedMessage)
        {
            return DoubleEncryptTypedMessage(typedMessage, SessionKey, ReceiverAsym, OutgoingAead());
        }

        
        public SymEncryptedBytes DoubleEncryptPayload(byte[] data, CryptoKey symKey, AsymKeyItem pubKey, byte[] associatedData)
        {
            var typedMessage = QCP.TypedMessage.Create(TunnelMessageType.PAYLOAD, data);
            return DoubleEncryptTypedMessage(typedMessage, symKey, pubKey, associatedData);
        }

        public SymEncryptedBytes DoubleEncryptTypedMessage(QCP.TypedMessage typedMessage, CryptoKey symKey, AsymKeyItem pubKey, byte[] associatedData)
        {
            QCP.TypedMessage typedMessage1 = typedMessage;
            if (pubKey != null)
            {
                // encrypt (ASYM with Key Id)
                var asymEncrypted = CryptoUtils.EncryptAsymWithKeyId(typedMessage.GetByteArray(), pubKey.PubKey);
                typedMessage1 = QCP.TypedMessage.Create(TunnelMessageType.ASYM_ENCRYPTED, asymEncrypted);
            }

            // encrypt (SYM) - bind Direction + MessageNumber + MessageProtocolVersion as Associated Data
            var res = CryptoUtils.SymEncrypt(typedMessage1, symKey, associatedData);
            return res;
        }
        public MsgHelper DoubleDecrypt(SymEncryptedBytes data, int messageNumber, string messageProtocolVersion)
        {
            return DoubleDecrypt(data, SessionKey, SenderAsym, IncomingAead(messageNumber, messageProtocolVersion));
        }

        public MsgHelper DoubleDecrypt(string dataString, int messageNumber, string messageProtocolVersion)
        {
            return DoubleDecrypt(dataString, SessionKey, SenderAsym, IncomingAead(messageNumber, messageProtocolVersion));
        }

        public MsgHelper DoubleDecrypt(string dataString, CryptoKey symKey, AsymKeyItem decryptionKey, byte[] associatedData)
        {
            var data = CR.SymEncryptedBytes.Create(dataString);
            return DoubleDecrypt(data, symKey, decryptionKey, associatedData);
        }

        public MsgHelper DoubleDecrypt(SymEncryptedBytes data, CryptoKey symKey, AsymKeyItem decryptionKey, byte[] associatedData)
        {
            // 1-st layer - decrypt with SYM key
            // 2-nd layer - decrypt with ASYM key (if we have it, otherwise return error)

            var res = new MsgHelper();
            
            // layer 1 - decrypt SYM - Direction + MessageNumber + MessageProtocolVersion are verified as Associated Data
            var layerSym = CryptoUtils.TrySymDecrypt(data, symKey, associatedData);
            if (layerSym.IsError())
                return res.SetError(layerSym.Message);

            res.TypedMessageSym = QCP.TypedMessage.Create(layerSym.Data.Bytes);

            if (res.IsDoubleEncrypted)
            {
                res.AsymEncrypted = CR.AsymEncryptedBytesWithKeyId.Create(res.TypedMessageSym.MessageBytes);
                if (decryptionKey == null)
                    return res.SetError("Cannot decrypt asymmetric tunnel payload because no decryption key is available");

                // check if we have the PubKey used for the 2-nd layer of encryption
                if (res.AsymEncrypted.KeyId != decryptionKey.KeyHashId)
                {
                    // different key, cannot decrypt
                    return res.SetError($"Asymmetric keys are not matching - payload message is encrypted with KeyHashId='{res.AsymEncrypted.KeyId}' but we are trying to decrypt it with KeyHashId='{decryptionKey.KeyHashId}'");
                }
                // layer 2 - decrypt with ASYM, have the right PrivKey
                var layerAsym = CryptoUtils.DecryptAsym(res.AsymEncrypted.AsymEncryptedBytes, decryptionKey.GetPairsPrivateKey());
                res.TypedMessageAsym = QCP.TypedMessage.Create(layerAsym);
            }

            return res;
        }

        #endregion

        #region Message Send / Poll - REST API call wrappers

        public async Task<HttpResponseModel<Model.MessageSendPollResponse>> MessagePoll(int timeoutMs = 120 * 1000)
        {
            // null as message will skip Send and will just do Poll
            return await MessageSendPoll(null, timeoutMs);
        }

        public async Task<HttpResponseModel<Model.MessageSendPollResponse>> MessageSend(string message)
        {
            // SEND - just send and return from the REST call, without checking/waiting in the Controller for the response message to come from the other side
            // the "-1" will be used to indicate that in request.ExpectResponse = false
            return await MessageSendPoll(message, -1);
        }

        public async Task<bool> IsServiceUp()
        {
            var response = await HttpGetAsync<QCP.StringObject>("api/QrsTunnel/Test");
            return response.IsSuccess();
        }

        public async Task<HttpResponseModel<Model.MessageSendPollResponse>> MessageSendPoll(string message, int pollingTimeoutMs)
        {
            this.MessageNumber++;
            return await SendPollWithCurrentMessageNumber(message, pollingTimeoutMs);
        }

        /// <summary>
        /// Sends (and optionally polls) using the already reserved (incremented) this.MessageNumber.
        /// The message must have been encrypted with the AEAD built from that very same MessageNumber.
        /// </summary>
        private async Task<HttpResponseModel<Model.MessageSendPollResponse>> SendPollWithCurrentMessageNumber(string message, int pollingTimeoutMs)
        {
            var request = new Model.MessageSendPollRequest();
            request.AppName = this.AppName;
            request.Direction = this.Direction;
            request.TunnelHashId = this.TunnelHashId;
            request.SenderPubKeyBytes = this.SenderPubKeyBytes;
            request.Message = message;
            request.MessageNumber = this.MessageNumber;
            request.MessageProtocolVersion = ClientProtocolVersion; // bound to Message via AEAD (see OutgoingAead)
            request.ExpectedResponseNumber = this.ExpectedResponseNumber;
            request.ExpectResponse = pollingTimeoutMs >= 0;
            request.PollingTimeout = pollingTimeoutMs;

#if DEBUG
            if (request.PollingTimeout < 15000)
                request.PollingTimeout = 15000;
#endif
            var response = await JsonPostAsync<Model.MessageSendPollResponse>(JsonOptions, "api/QrsTunnel/MessageSendPoll", request, GetHttpClient(pollingTimeoutMs));
            if (response.IsError())
                // error or timeout while Send
                return ErrorResponse<Model.MessageSendPollResponse>(response);

            var serverVersionError = ValidateServerProtocolVersion(response.Data?.ServerProtocolVersion);
            if (!string.IsNullOrWhiteSpace(serverVersionError))
                return ErrorResponse<Model.MessageSendPollResponse>(serverVersionError);

            if (response.Data.IsTimeout())
                // error - timeout indicated in Data.Status
                return ErrorResponse<Model.MessageSendPollResponse>("Timeout - no response from QRS mobile application");

            if (ExpectedResponseNumber > response.Data.MessageNumber && response.Data.MessageNumber > 0)
                // error after Success - unexpected Message Number (ignore those few flows that purposedly send negative message number)
                return ErrorResponse<Model.MessageSendPollResponse>($"Unexpected MessageNumber in response ({response.Data.MessageNumber}) while expecting something >= ({ExpectedResponseNumber})");

            if (response.Data.MessageNumber > 0)
                ExpectedResponseNumber = response.Data.MessageNumber + 1;


            return response;
        }

        private HttpClient GetHttpClient(int timeoutMs = 0)
        {
            // this "timeoutMs" is NOT a client-side HttpClient timeout.
            // this is a parameter passed on to the Server telling what should be a polling timeout for a server-side Long Polling Task.
            // however it also influences a choise between 2 pre-create HttpClient objects precreated with "long" 120 sec and "short" client-side timeouts.
            //
            // timeout cannot be changed after HttpClient has been created
            // so we have a _pollingClient created with 120 sec timeout
            // and default client with 10 sec timeout
            // whenever intended timeout is > 10 sec, return _pollingClient
            // otherwise, return null, which will result into using default client
            return timeoutMs > 10000 ? _pollingClient : null;
        }

        private string EnsureReceiverPublicKeyForQrV2(Model.MessageSendPollResponse response)
        {
            if (Direction != Directions.MA2CA)
                return null;

            if (response.SenderPubKeyBytes == null || response.SenderPubKeyBytes.Length == 0)
            {
                return ReceiverAsym == null
                    ? "QRS QR v2 requires sender public key in the tunnel response"
                    : null;
            }

            if (string.IsNullOrWhiteSpace(TunnelHashId))
                return "QRS QR v2 requires client public key hash delivered via QR code ('clientPubHash' param)";

            try
            {
                var senderPubKeyHashId = CryptoUtils.GetTunnelHashId(response.SenderPubKeyBytes);
                if (senderPubKeyHashId != TunnelHashId)
                {
                    LogDebug($"Corrupted tunnel - hash from QR code: '{TunnelHashId}' does not match the hash of SenderPubKeyBytes provided by tunnel: '{senderPubKeyHashId}'");
                    return "Corrupted tunnel - hash from QR code does not match the public key provided by tunnel";
                }
                var senderPubKey = CR.CryptoKey.Create(response.SenderPubKeyBytes);
                if (ReceiverAsym == null)
                    ReceiverAsym = new AsymKeyItem(senderPubKey);
            }
            catch (Exception ex)
            {
                return $"Cannot load sender public key from QRS tunnel response: {ex.Message}";
            }

            return null;
        }

        #endregion

    }


}
