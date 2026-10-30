using System;
using System.Collections.Concurrent;
using System.Text;
using System.Timers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qrid.Common.Analytics;
using Qrs.Tunnel.Server.Models;
using Model = Qrs.Tunnel.Shared.Models;
using Qrs.Tunnel.Shared.Models;
using Qrs.Tunnel.Shared;

namespace Qrs.Tunnel.Server.Services
{
    // Holds the zero-knowledge tunnel session state and message routing logic.
    // Previously this lived in the MVC controller QrsTunnelController; it was
    // extracted here so the HTTP surface can be expressed as Native-AOT-friendly
    // Minimal API endpoints (see Program.cs). All tunnel payloads remain opaque
    // ciphertext to the server.
    public static class TunnelService
    {
        // Default number of concurrent tunnel sessions allowed; can be overridden via
        // configuration key "QrsTunnel:MaxSessionCount" (see appsettings.json / env vars).
        private const int DEFAULT_MAX_SESSION_COUNT = 10000;
        private static int _maxSessionCount = DEFAULT_MAX_SESSION_COUNT;

        // Optional API key that protects the session-creation flow from memory-overflow
        // (too-many-sessions) attacks. Configured via "QrsTunnel:ApiKey" (appsettings.json)
        // or the "QrsTunnel__ApiKey" environment variable. When null/empty the check is
        // disabled and the server behaves exactly as before. When set, a request that would
        // CREATE a new tunnel session must present a matching api key cookie; requests that
        // only read from or write to an already existing session are never checked, so the
        // Mobile App (which never creates a session) does not need the key.
        private static string? _apiKey = null;

        private const int MAX_SESSION_DURATION_MINUTES = -6;
        private const int MAX_CLIENT_POLLING_TIMEOUT_MS = 4 * 60 * 1000; // hard cap for client-supplied PollingTimeout

#if DEBUG
        public static int _pollingTimeout = 20 * 60 * 1000; // 20 min in Debug builds to make manual/step-through debugging easier
#else
        public static int _pollingTimeout = 2 * 60 * 1000; // 2 min in Release builds
#endif

        private static readonly ConcurrentDictionary<string, TunnelSessionItem> _tunnelSessions = new ConcurrentDictionary<string, TunnelSessionItem>();
        private static readonly ConcurrentQueue<TunnelSessionItem> _sessionQueue = new ConcurrentQueue<TunnelSessionItem>();
        private static System.Timers.Timer? _cleanupTimer; // timer is used to clean up expired _tunnelSessions
        private static readonly Stats _stats = new Stats();

        private static ConcurrentDictionary<string, SessionLogItem> _sessionLogs = new ConcurrentDictionary<string, SessionLogItem>();

        public static Stats Stats => _stats;

        // Reads the configurable session cap once at application startup (see Program.cs).
        public static void Configure(IConfiguration configuration)
        {
            _maxSessionCount = int.TryParse(configuration["QrsTunnel:MaxSessionCount"], out var maxSessionCount)
                ? maxSessionCount
                : DEFAULT_MAX_SESSION_COUNT;

            // Optional api key protecting the session-creation flow. Environment variables
            // (QrsTunnel__ApiKey) are picked up automatically by IConfiguration.
            var apiKey = configuration["QrsTunnel:ApiKey"];
            _apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();
        }

        // True when an api key has been configured and must be presented to create a session.
        public static bool IsApiKeyRequired => _apiKey != null;

        // Returns true when the api key check is disabled, or when the supplied value matches
        // the configured key. Uses a constant-time comparison to avoid leaking the key via
        // timing analysis.
        public static bool IsApiKeyValid(string? apiKey)
        {
            if (!IsApiKeyRequired)
                return true;

            if (string.IsNullOrEmpty(apiKey))
                return false;

            var provided = Encoding.UTF8.GetBytes(apiKey.Trim());
            var expected = Encoding.UTF8.GetBytes(_apiKey!);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(provided, expected);
        }


        public static bool IsApiKeyValidForAdmin(string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                return false;

            if (string.IsNullOrWhiteSpace(apiKey))
                return false;

            var provided = Encoding.UTF8.GetBytes(apiKey.Trim());
            var expected = Encoding.UTF8.GetBytes(_apiKey!);
            return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(provided, expected);
        }

        public static List<SessionLogItem> GetSessionLogs()
        {
            return _sessionLogs.Values.OrderByDescending(s => s.Created).ToList();
        }

        public static int ClearSessionLogs()
        {
            var oldLogs = _sessionLogs;
            _sessionLogs = new ConcurrentDictionary<string, SessionLogItem>();

            var clearedCount = oldLogs.Count;
            oldLogs.Clear();
            return clearedCount;
        }

        private static string GetLogKey(int sessionId, string instanceId)
        {
            return $"{sessionId}:{instanceId}";
        }

        public static async Task<TunnelResponse<Model.MessageSendPollResponse>> MessageSendPollAsync(
            Model.MessageSendPollRequest request,
            string instanceId,
            string clientIp,
            string apiKey,
            ILogger logger)
        {
            if (request.Direction != Directions.CA2MA && request.Direction != Directions.MA2CA)
                return ErrorResponse<Model.MessageSendPollResponse>($"Invalid request.Direction='{request.Direction}'");

            var requestString = System.Text.Json.JsonSerializer.Serialize(request, Json.TunnelJsonContext.Default.MessageSendPollRequest);

            // TODO - make this configurable
            if (requestString.Length > 200000)
                return ErrorResponse<Model.MessageSendPollResponse>($"Request is too large - {requestString.Length.ToString("n0")} while maximum allowed is 200,000");

            // SESSION

            // The optional api key is only enforced when a NEW session must be created (see
            // GetOrCreateSession). Reads/writes to an existing session are never blocked.
            var apiKeyValid = IsApiKeyValid(apiKey);
            var session = GetOrCreateSession(request.TunnelHashId, apiKeyValid, logger);
            if (session == null)
            {
                if (IsApiKeyRequired && !apiKeyValid)
                    return ErrorResponse<Model.MessageSendPollResponse>($"A valid API key is required to open a new Tunnel Session on this server.");
                else if (IsTooManySessions())
                    return ErrorResponse<Model.MessageSendPollResponse>($"Tunnel Server is overloaded at the moment. Please try again later!");
                else
                    return ErrorResponse<Model.MessageSendPollResponse>($"Cannot find nor create Tunnel Session for this PubKey='{request.TunnelHashId}'");
            }
            // Session id
            var sessionId = request.TunnelHashId.GetHashCode();
            // Session Log
            var logKey = GetLogKey(sessionId, instanceId);
            if (!_sessionLogs.ContainsKey(logKey))
            {
                var logItem = new SessionLogItem();
                logItem.Created = DateTime.Now;
                logItem.App = request.AppName;
                logItem.Ip = clientIp;
                logItem.InstanceId = instanceId;
                logItem.SessionId = sessionId;
                // add to Session Log
                _sessionLogs.TryAdd(logKey, logItem);
            }


            var effectivePollingTimeoutMs = Math.Min(request.PollingTimeout, MAX_CLIENT_POLLING_TIMEOUT_MS);

            logger.LogDebug("=");
            logger.LogDebug("=");
            logger.LogDebug("========== Direction = {Direction} PubKeyHash = {SessionId} MessageNumber = {MessageNumber} TimeoutRequested = {TimeoutRequested} TimeoutEffective = {TimeoutEffective} ===================================", request.Direction, sessionId, request.MessageNumber, request.PollingTimeout, effectivePollingTimeoutMs);
            logger.LogDebug("=");
            logger.LogDebug("==== Request: ====");
            logger.LogDebug("{Request}", requestString);

            var response = new Model.MessageSendPollResponse();
            response.ServerProtocolVersion = QrsTunnelProtocol.ServerProtocolVersion;

            // SENDING

            if (request.HasMessageToSend())
            {
                var sendingDirection = session.GetSendingDirection(request.Direction);

                // save message into TunnelSession
                lock (session)
                {
                    if (request.MessageNumber > sendingDirection.MessageNumber)
                    {
                        sendingDirection.MessageNumber = request.MessageNumber;
                        sendingDirection.Message = request.Message;
                        sendingDirection.SenderPubKeyBytes = request.SenderPubKeyBytes;
                        sendingDirection.MessageProtocolVersion = request.MessageProtocolVersion;
                    }
                    else
                        return ErrorResponse<Model.MessageSendPollResponse>($"Error while sending message (request.Direction='{request.Direction}') - request.MessageNumber ({request.MessageNumber}) is NOT greater than previous MessageNumber ({sendingDirection.MessageNumber}) and so request is rejected and message has not been sent.");
                }

                // complete all tasks that await results for this TunnelSession on this Direction
                var ptr = new PollingTaskResult { Error = string.Empty, Message = request.Message, SenderPubKeyBytes = request.SenderPubKeyBytes, MessageNumber = request.MessageNumber, MessageProtocolVersion = request.MessageProtocolVersion };
                while (sendingDirection.AwaitingTasks.TryTake(out var sawt))
                {
                    if (!sawt.PollingTaskSource.Task.IsCompleted)
                        sawt.PollingTaskSource.TrySetResult(ptr);
                }

            }


            // POLLING FOR RESULTS

            if (!request.ExpectResponse)
            {
                response.Message = null;
                response.MessageNumber = -1;
                response.Status = "SUCCESS";
                logger.LogDebug("ExpectResponse==false, so return without waiting");
                return SuccessResponse(response);
            }

            var receivingDirection = session.GetReceivingDirection(request.Direction);

            if (receivingDirection.MessageNumber >= request.ExpectedResponseNumber)
            {
                // success, the results are already available, no need to poll
                logger.LogDebug("results are already available - receivingDirection.MessageNumber({ReceivedNumber}) >= request.ExpectedResponseNumber({ExpectedNumber})", receivingDirection.MessageNumber, request.ExpectedResponseNumber);
                response.Message = receivingDirection.Message;
                response.SenderPubKeyBytes = receivingDirection.SenderPubKeyBytes;
                response.MessageNumber = receivingDirection.MessageNumber;
                response.MessageProtocolVersion = receivingDirection.MessageProtocolVersion;
                response.Status = "SUCCESS";
                return SuccessResponse(response);
            }

            // need to poll b/c expected results are not yet here

            if (effectivePollingTimeoutMs <= 0)
            {
                //
                // timeout, no results
                response.Message = null;
                response.MessageNumber = -2;
                response.Status = "TIMEOUT";
                logger.LogDebug("INSTANT TIMEOUT - results were not avail immediately, and timeout was not specified - so here we have instant TIMEOUT without waiting for results. Specify >= 0 timeout in request");
                return SuccessResponse(response);
            }

            // POLL waiting for results

            // 1) add new AwaitingTask
            var pollingTcs = new TaskCompletionSource<PollingTaskResult>();
            var at = new AwaitingTaskItem { PubKey = request.TunnelHashId, CreateDate = DateTime.Now, PollingTaskSource = pollingTcs };
            receivingDirection.AwaitingTasks.Add(at); // this will be used by SendMessage() to notify us that result is available

            logger.LogDebug("waiting for QR code to be scanned... PubKey='{PubKey}' bs.AwaitingTasks.Count={Count}", request.TunnelHashId, receivingDirection.AwaitingTasks.Count);

            // 2) await for it's result
            await Task.WhenAny(Task.Delay(effectivePollingTimeoutMs), pollingTcs.Task);
            if (pollingTcs.Task.IsCompleted)
            {
                // have result
                if (pollingTcs.Task.Result.MessageNumber >= request.ExpectedResponseNumber)
                {
                    // all good
                    response.Message = pollingTcs.Task.Result.Message;
                    response.SenderPubKeyBytes = pollingTcs.Task.Result.SenderPubKeyBytes;
                    response.MessageNumber = pollingTcs.Task.Result.MessageNumber;
                    response.MessageProtocolVersion = pollingTcs.Task.Result.MessageProtocolVersion;
                    response.Status = "SUCCESS";
                }
                else
                {
                    // message number is below expected
                    return ErrorResponse<Model.MessageSendPollResponse>($"Error while receiving message - request.ExpectedResponseNumber ({request.ExpectedResponseNumber}) IS GREATER THAN received MessageNumber ({pollingTcs.Task.Result.MessageNumber}) and so we have discarded the received message");
                }
            }
            else
            {
                // timeout, no results
                response.Message = null;
                response.MessageNumber = -3;
                response.Status = "TIMEOUT";
            }

            return SuccessResponse(response);
        }


        private static TunnelResponse<T> ErrorResponse<T>(string message)
            => TunnelResponse<T>.Error(message);

        private static TunnelResponse<T> SuccessResponse<T>(T? data = default, string? message = null)
            => TunnelResponse<T>.Success(data, message);

        public static bool IsTooManySessions()
        {
            return _tunnelSessions.Count >= _maxSessionCount;
        }

        public static TunnelSessionItem? GetOrCreateSession(string publicKey, bool apiKeyValid, ILogger logger)
        {
            if (_tunnelSessions.TryGetValue(publicKey, out var existingSession))
            {
                logger.LogDebug("GetOrCreateSession - returning existing session for PubKey='{PubKey}'", publicKey);
                return existingSession;
            }
            else
            {
                // A new session would be created below. Enforce the optional api key here
                // (and only here) so that reads/writes to existing sessions are never blocked.
                if (IsApiKeyRequired && !apiKeyValid)
                {
                    logger.LogDebug("API KEY MISSING OR INVALID - refusing to create session for PubKey='{PubKey}'", publicKey);
                    return null;
                }

                if (IsTooManySessions())
                {
                    logger.LogDebug("TOO MANY SESSIONS - returning NULL for PubKey='{PubKey}'", publicKey);
                    return null;
                }

                var session = new TunnelSessionItem(publicKey);
                if (_tunnelSessions.TryAdd(publicKey, session))
                {
                    _sessionQueue.Enqueue(session);
                    logger.LogDebug("GetOrCreateSession - returning NEW session for PubKey='{PubKey}'", publicKey);
                    return session;
                }
                else
                {
                    logger.LogDebug("GetOrCreateSession - failed to TryAdd session - returning NULL for PubKey='{PubKey}'", publicKey);
                    return null;
                }

            }
        }

        public static bool Exists(string publicKey)
        {
            return _tunnelSessions.ContainsKey(publicKey);
        }

        public static string GetAllSessionsKeys()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var s in _tunnelSessions)
            {
                sb.Append(s.Key);
                sb.Append(", ");
            }

            return sb.ToString();
        }

        public static void CleanupSession(string publicKey)
        {
            if (_tunnelSessions.TryGetValue(publicKey, out var session))
            {
                session.DirectionCaToMa?.CancelTasks();
                session.DirectionMaToCa?.CancelTasks();
                session.DirectionCaToMa = null;
                session.DirectionMaToCa = null;
            }
        }

        public static void CleanupSessions(out int successCount, out int dqErrors, out int removeErrors)
        {
            // will return a count of cleaned up Sessions
            successCount = 0;
            dqErrors = 0;
            removeErrors = 0;
            var done = false;
            var cutoff = DateTime.Now.AddMinutes(MAX_SESSION_DURATION_MINUTES);
            while (!done)
            {
                if (_sessionQueue.TryPeek(out var session))
                {
                    if (session.CreateDate > cutoff) return;

                    // dequeue session
                    if (_sessionQueue.TryDequeue(out var sessionDequeued))
                    {
                        // clear session
                        CleanupSession(session.PublicKey);
                        // remove from session dictionary
                        if (_tunnelSessions.TryRemove(session.PublicKey, out var sessionRemoved))
                        {
                            // all good
                            successCount++;
                        }
                        else
                        {
                            removeErrors++;
                            return;
                        }
                    }
                    else
                    {
                        dqErrors++;
                        return;
                    }
                }
                else
                    return;
            }
        }


        public static void StartCleanupTimer()
        {
            if (_cleanupTimer != null) return;

            // Create a timer with a 10 seconds interval
            _cleanupTimer = new System.Timers.Timer(10000);
            _cleanupTimer.Elapsed += OnCleanupTimerEvent;
            _cleanupTimer.AutoReset = true;
            _cleanupTimer.Enabled = true;
        }

        // OnCleanupTimerEvent
        public static void RunCleanup()
        {
            OnCleanupTimerEvent(null, null);
        }

        private static void OnCleanupTimerEvent(object? source, ElapsedEventArgs? e)
        {
            CleanupSessions(out int successCount, out int dqErrors, out int removeErrors);
            _stats.TimerTicks++;
            _stats.TotalSessionCount += successCount;
            _stats.DequeueErrorsCount += dqErrors;
            _stats.RemoveErrorsCount += removeErrors;
            _stats.ActiveSessionCount = _tunnelSessions.Count;
        }
    }
}
