# 11. Class Diagrams

UML class diagrams (Mermaid) for the main QRS components. Diagrams show the members that matter
for understanding the design; private helpers are omitted.

- [Overview: who uses whom](#overview-who-uses-whom)
- [Tunnel client](#tunnel-client)
- [Client SDK (Client Application side)](#client-sdk-client-application-side)
- [Mobile App engine](#mobile-app-engine)
- [Tunnel Server](#tunnel-server)
- [Tunnel wire DTOs](#tunnel-wire-dtos)
- [Protobuf: QRS messages](#protobuf-qrs-messages)
- [Protobuf: crypto containers](#protobuf-crypto-containers)
- [Reference app wrappers](#reference-app-wrappers)

---

## Overview: who uses whom

```mermaid
classDiagram
    direction LR
    class ClientContext
    class QrsAppContext
    class TunnelClient
    class TunnelService
    class CryptoUtils
    class WrapperBase
    class IUserInteraction
    class IAsymKeyItemProvider
    class IDevice

    ClientContext --> TunnelClient : CA2MA
    QrsAppContext --> TunnelClient : MA2CA
    TunnelClient ..> TunnelService : HTTP MessageSendPoll
    ClientContext --> IUserInteraction
    ClientContext --> IAsymKeyItemProvider
    ClientContext ..> WrapperBase : wraps / unwraps
    QrsAppContext --> IDevice : secure storage
    TunnelClient ..> CryptoUtils
    ClientContext ..> CryptoUtils
    QrsAppContext ..> CryptoUtils
```

## Tunnel client

```mermaid
classDiagram
    class CommonContext {
        +SuccessResponse()
        +ErrorResponse(message)
        +SuccessStringResponse(value)
        +ErrorStringResponse(error)
    }
    class CommonApiClient {
        +string BaseUrl
        +string AppName
        +ICookieProvider CookieProvider
        +JsonPostAsync(options, path, request, client)
        +HttpGetAsync(path)
    }
    class QrsApiClient
    class TunnelClient {
        +const string ClientProtocolVersion = "1.0.0"
        +string Direction
        +CryptoKey SessionKey
        +AsymKeyItem SenderAsym
        +AsymKeyItem ReceiverAsym
        +byte[] SenderPubKeyBytes
        +string TunnelHashId
        +int MessageNumber
        +int ExpectedResponseNumber
        +bool DoNotResetMessageNumbers
        +string PeerMessageProtocolVersion
        +bool IsPeerMessageProtocolVersionPinned
        +string ApiKey
        +InitClientApplication()
        +InitMobileApplication(QrCommand, AsymKeyItem)
        +string GetQrString(cmd)
        +ValidateTunnelUrl(url)$ string
        +EncryptSendPollDecrypt(ProtoBase, timeoutMs) Task
        +EncryptSendPollDecryptTypedMessage(TypedMessage, timeoutMs) Task
        +EncryptSend(ProtoBase) Task
        +EncryptSendTypedMessage(TypedMessage) Task
        +PollDecrypt(timeoutMs) Task
        +MessagePoll(timeoutMs) Task
        +DoubleEncryptTypedMessage(...) SymEncryptedBytes
        +DoubleDecrypt(...) MsgHelper
        +GetTunnelAead(direction, number, version)$ byte[]
        +OutgoingAead() byte[]
        +IncomingAead(number, version) byte[]
        +CancelRequest() bool
    }
    class AsymKeyItem {
        +CryptoKeyPair KeyPair
        +CryptoKey PubKey
        +string PubKeyString
        +string KeyHashId
        +GetPairsPrivateKey() CryptoKey
        +GetPairsPublicKey() CryptoKey
    }
    class MsgHelper {
        +string ErrorMessage
        +TypedMessage TypedMessageSym
        +TypedMessage TypedMessageAsym
        +AsymEncryptedBytesWithKeyId AsymEncrypted
        +TypedMessage TypedMessage
        +bool IsError
        +bool IsDoubleEncrypted
        +bool IsKeyIdDifferent
        +bool IsPayload
        +bool IsSwitchRequested
        +bool IsSwitchRejected
        +bool IsPairingRequestedByMa
        +bool IsPairingRequestedByCa
        +GetPairingRequest() PairingRequest
        +GetSwitchPubKeyRequest() string
    }
    class TunnelMessageType {
        <<static>>
        PAYLOAD = "Payload"
        ASYM_ENCRYPTED = "AEBKID"
        PAIRING_REQUESTED_CA
        PAIRING_REQUESTED_MA
        SWITCH_PUBKEY_REQUESTED
        SWITCH_PUBKEY_REJECTED
    }
    class IAsymKeyItemProvider {
        <<interface>>
        +GetAllAsymKeyItems() List
        +Get(keyId) AsymKeyItem
        +SaveAsymKeyItem(item)
        +GetMostRecentKeyId() string
        +SetMostRecentKeyId(keyId)
    }
    class FileAsymKeyItemProvider {
        +string Folder
    }
    class QrKeyValueBase {
        +Dictionary Params
        +string Error
        +bool IsValid
        +GetParam(key) string
        +SetParam(key, value)
        +Parse(qr)
        +ToQrString() string
    }
    class QrCommand {
        +string QrVer
        +string Cmd
        +string Server
        +string ClientPubHash
        +string SessionKey
        +string ClientProtocolVersion
        +Create(qr)$ QrCommand
    }
    CommonContext <|-- CommonApiClient
    CommonApiClient <|-- QrsApiClient
    QrsApiClient <|-- TunnelClient
    TunnelClient o-- AsymKeyItem : SenderAsym / ReceiverAsym
    TunnelClient ..> MsgHelper : returns
    TunnelClient ..> TunnelMessageType
    TunnelClient ..> QrCommand
    QrKeyValueBase <|-- QrCommand
    IAsymKeyItemProvider <|.. FileAsymKeyItemProvider
    IAsymKeyItemProvider ..> AsymKeyItem
```

## Client SDK (Client Application side)

```mermaid
classDiagram
    class ClientContext {
        +string AppName
        +int CommandCount
        +bool CanPair
        +bool CanSwitchPubKey
        +string Domain
        +QrsConfiguration Config
        +string ApiKey
        +IUserInteraction UI
        +IAsymKeyItemProvider AsymKeyItemProvider
        +ICookieProvider CookieProvider
        +TunnelClient TunnelClient
        +InitFromConfigAsync(domain, uriTemplate, apiKey) Task
        +InitDomainAsync(domain) Task
        +InitDomain(uriTemplate)
        +ExecuteQrCommandAsync(cmd, request) Task
        +MainFlowAsync(request) Task
        +PairingFlowAsync(pairingRequest, message) Task
        +SwitchPubKey(keyId) bool
        +KeyOpAsync(KeyOpRequest) Task
        +WrapNewPayload(wrappers) Task
        +WrapUpdatedPayload(wrappers) Task
        +UnwrapPayload(wrappers) Task
        +CancelQrCodePolling() bool
    }
    class WrapperBase {
        +string AppName
        +QrsEnvelope QrsEnvelope
        +AssociatedData2 ADFromEnvelope
        +AssociatedData2 ADFromData
        +IdentityData IDPAY
        +CryptoKey DEK
        +ResponseModel Status
        +bool IsModifiedPayload
        +bool IsModifiedAssociatedData
        +GetPayloadType()* string
        +GetPayloadBytes()* byte[]
        +SetPayload(TypedMessage)* string
        +GetAssociatedData2()* AssociatedData2
        +GetImmutableDekId() ByteString
        +GetImmutableTopLevelKey() int
        +GenerateNewDekOnModifiedPayload bool
        +GetPayloadMessage() TypedMessage
        +IsValidIdentityData(out error) bool
    }
    class Wrapper~TPayload~ {
        +TPayload Payload
        +CreatePayload()
        +ClearPayload()
    }
    class RequestHelper {
        +bool IsModifiedPayload
        +bool IsModifiedAssociatedData
        +bool IsDekOpen
        +bool GenerateNewDekOnModifiedPayload
        +bool NeedsOldKey
        +bool NeedsNewKey
        +bool NeedsRewrapWithModifiedAd
        +bool NeedsMobileApp
        +KeyOp KeyOp
        +Calculate()
    }
    class IUserInteraction {
        <<interface>>
        +ShowQrCodeWindow(sessionId, qr)
        +ShowQrCodeWindowAsync(sessionId, qr) Task
        +HideQrCodeWindow(sessionId)
        +GetUserInputAsync(inputContext) Task
        +ShowMessage(msg)
        +ShowError(msg)
        +ShowMessageWindow(title)
        +HideMessageWindow()
        +SetDescription(title, subtitle)
        +SetTunnelServiceUri(uri)
        +ShowUsernameWindow(callback)
    }
    class DiscoveryClient {
        +Instance$ DiscoveryClient
        +GetDomainQrsConfigurationAsync(domain) Task
        +GetServiceUriAsync(domain, service) Task
    }
    class VaultContext {
        +const APP_NAME = "QRS VAULT"
        +VaultWrapper VaultWrapper
        +Settings Settings
        +CreateInitialized(root, ui)$ Task
    }
    CommonContext <|-- ClientContext
    ClientContext <|-- VaultContext
    ClientContext --> TunnelClient
    ClientContext --> IUserInteraction
    ClientContext --> IAsymKeyItemProvider
    ClientContext ..> DiscoveryClient
    ClientContext ..> RequestHelper
    ClientContext ..> WrapperBase
    WrapperBase <|-- Wrapper~TPayload~
    WrapperBase --> QrsEnvelope
```

## Mobile App engine

```mermaid
classDiagram
    class QrsAppContext {
        +const APP_NAME = "QRS"
        +TunnelClient TunnelClient
        +NewUserFlow NewUserFlow
        +ExistingUserFlow ExistingUserFlow
        +BackupFlow BackupFlow
        +QrCommandFlow QrCommandFlow
        +OrangeKeys OrangeKeys
        -AsymKeyItem DeviceKeyItem
        +bool IsLoggedIn
        +Init() bool
        +InitAsync() Task
        +ResetPairing()
        +NewMasterKeyAsync(password, keyName) Task
        +ExistingMasterKeyAsync(password, qr, keyName, useKeyNameAsAD) Task
        +BackupMasterKeyAsync(password) Task
        +ParseBackupImage(text) QrBackupImage
        +Logout()
        +DownloadRequestAsync(QrCommand) Task
        +PayloadOrPairing() Task
        +StartPairing() Task
        +KeyOp() KeyOpResponse
        +QrCommandSendResponseAsync(response) Task
        +GetQrCodeString(peb, keyName) string
    }
    class QrCommandFlow {
        +QrCommand QrCommand
        +string ErrorMessage
        +Dictionary ValidationErrors
        +Dictionary FatalErrors
        +TypedMessage Payload
        +KeyOpRequest KeyOpRequest
        +List~RequestItem~ RequestItems
        +PairingRequest PairingRequest
        +List~string~ Alerts
        +string PendingKnownServer
        +Init(QrCommand)
        +ClearRequest()
        +ParsePayload(kekName)
        +ValidateCommand(loadedKekName)
        +IsValidCommand() bool
        +IsImpossibleToContinueCommand() bool
        +IsCanonicalAppName(name) bool
    }
    class RequestItem {
        +KeyOpRequestItem KeyOpRequestItem
        +KeyOpResponseItem KeyOpResponseItem
        +IdentityData IdentityData
        +AssociatedData2 OldAssociatedData
        +AssociatedData2 NewAssociatedData
        +AssociatedData2 DisplayAssociatedData
        +string ErrorMessage
        +bool IsError
    }
    class OrangeKeys {
        +CryptoKey MasterKeySym
        +string KeyName
    }
    class NewUserFlow {
        +CryptoKey MasterKeySym
        +PasswordEncryptedBytes PasswordEncryptedMasterKey
        +string KeyName
    }
    class ExistingUserFlow {
        +QrBackupImage QrBackupImage
        +OrangeKeys OrangeKeysCandidate
    }
    class BackupFlow {
        +string QrCodeString
    }
    class MobUtils {
        <<static>>
        +IDevice Device
        +SaveOrangeKeys(keys)
        +LoadOrangeKeys() OrangeKeys
        +LoadOrGenerateDeviceKeyItem() AsymKeyItem
        +ReGenerateDeviceKeyItem() AsymKeyItem
        +LoadKnownServers() List
        +AddKnownServer(url)
        +ClearKnownServers()
    }
    class IDevice {
        <<interface>>
        +SecureStorageSet(key, value)
        +SecureStorageGet(key) string
        +SecureStorageClear()
    }
    class MobileDevice
    class QrBackupImage {
        +string Key
        +string Name
        +bool ContainsName
        +bool IsValidBackupImage
    }
    CommonContext <|-- QrsAppContext
    QrsAppContext *-- QrCommandFlow
    QrsAppContext *-- NewUserFlow
    QrsAppContext *-- ExistingUserFlow
    QrsAppContext *-- BackupFlow
    QrsAppContext o-- OrangeKeys
    QrsAppContext --> TunnelClient
    QrsAppContext ..> MobUtils
    QrCommandFlow *-- RequestItem
    MobUtils --> IDevice
    IDevice <|.. MobileDevice
    ExistingUserFlow --> QrBackupImage
```

## Tunnel Server

```mermaid
classDiagram
    class Program {
        +Main(args)$
        -MessageSendPollAsync(ctx)$
        -Test()$
        -Version()$
        -IsAdmin(ctx)$ bool
        -Stats(ctx)$
        -Cleanup(ctx)$
        -PrintHeaders(ctx)$
        -GetSessionLog(ctx)$
        -ClearSessionLog(ctx)$
    }
    class TunnelService {
        <<static>>
        -ConcurrentDictionary _tunnelSessions
        -ConcurrentQueue _sessionQueue
        -Timer _cleanupTimer
        -Stats _stats
        -ConcurrentDictionary _sessionLogs
        -int _maxSessionCount
        -string _apiKey
        +Configure(IConfiguration)$
        +MessageSendPollAsync(request, instanceId, ip, apiKey, logger)$ Task
        +GetOrCreateSession(id, apiKeyValid, logger)$ TunnelSessionItem
        +IsApiKeyValid(key)$ bool
        +IsApiKeyValidForAdmin(key)$ bool
        +IsTooManySessions()$ bool
        +CleanupSessions(out ok, out dq, out rm)$
        +StartCleanupTimer()$
        +GetSessionLogs()$ List
        +ClearSessionLogs()$ int
    }
    class TunnelSessionItem {
        +string PublicKey
        +DateTime CreateDate
        +DirectionItem DirectionMaToCa
        +DirectionItem DirectionCaToMa
        +GetSendingDirection(dir) DirectionItem
        +GetReceivingDirection(dir) DirectionItem
    }
    class DirectionItem {
        +string Direction
        +ConcurrentBag~AwaitingTaskItem~ AwaitingTasks
        +string Message
        +byte[] SenderPubKeyBytes
        +int MessageNumber
        +string MessageProtocolVersion
        +DateTime MessageDate
        +CancelTasks()
    }
    class AwaitingTaskItem {
        +TaskCompletionSource~PollingTaskResult~ PollingTaskSource
        +string PubKey
        +DateTime CreateDate
    }
    class PollingTaskResult {
        +string Message
        +byte[] SenderPubKeyBytes
        +int MessageNumber
        +string MessageProtocolVersion
        +string Error
    }
    class Stats {
        +long TimerTicks
        +int ActiveSessionCount
        +int TotalSessionCount
        +int DequeueErrorsCount
        +int RemoveErrorsCount
        +DateTime Created
    }
    class SessionLogItem {
        +DateTime Created
        +string App
        +string Ip
        +string InstanceId
        +int SessionId
    }
    class TunnelResponse~T~ {
        +string Status
        +T Data
        +string Message
        +string ServerProtocolVersion
        +Success(data, message)$
        +Error(error)$
    }
    class TunnelJsonContext {
        <<JsonSerializerContext>>
    }
    Program ..> TunnelService
    Program ..> TunnelJsonContext
    TunnelService *-- TunnelSessionItem
    TunnelService *-- Stats
    TunnelService *-- SessionLogItem
    TunnelSessionItem *-- "2" DirectionItem
    DirectionItem *-- AwaitingTaskItem
    AwaitingTaskItem ..> PollingTaskResult
    TunnelService ..> TunnelResponse~T~
```

## Tunnel wire DTOs

```mermaid
classDiagram
    class MessageSendPollRequest {
        +string TunnelHashId
        +byte[] SenderPubKeyBytes
        +string AppName
        +string Message
        +int MessageNumber
        +string MessageProtocolVersion
        +string Direction
        +int PollingTimeout
        +int ExpectedResponseNumber
        +bool ExpectResponse
        +HasMessageToSend() bool
    }
    class MessageSendPollResponse {
        +string Message
        +byte[] SenderPubKeyBytes
        +int MessageNumber
        +string MessageProtocolVersion
        +string Status
        +string ServerProtocolVersion
        +IsTimeout() bool
    }
    class VersionResponse {
        +string ServerProtocolVersion
        +string BuildVersion
        +string BuildTime
    }
    class Directions {
        <<static>>
        CA2MA
        MA2CA
    }
    class QrsTunnelProtocol {
        <<static>>
        +ServerProtocolVersion = "1.0.0"
        +ClientProtocolName
        +ServerProtocolName
        +TryParse(version, out semver)$ bool
        +ValidateVersion(name, received, own, required)$ string
        +ValidateServerProtocolVersion(received)$ string
    }
    MessageSendPollRequest ..> Directions
```

## Protobuf: QRS messages

See [Key Operations](07-key-operations.md#keyoprequest--keyopresponse) for the diagram of
`KeyOpRequest`, `KeyOpResponse`, `QrsEnvelope`, `IdentityData` and `AssociatedData2`. Pairing:

```mermaid
classDiagram
    class PairingRequest {
        +CryptoKey PubKey
        +string KeyId
        +string PairingCode
    }
    class PairingResponse {
        <<reserved>>
        +CryptoKey PubKey
        +string KeyId
        +string Status
        +string Message
    }
    class TypedMessage {
        +string MessageType
        +bytes MessageBytes
    }
    class StringObject {
        +string Value
    }
    TypedMessage ..> PairingRequest : Pairing-Requested-MA
    TypedMessage ..> StringObject : SwitchPubKey-Requested
```

## Protobuf: crypto containers

```mermaid
classDiagram
    class CryptoKey {
        +KeyAlgo KeyAlgo
        +KeyType KeyType
        +bytes KeyBytes
        +List~KeyUsage~ KeyUsage
    }
    class CryptoKeyPair {
        +CryptoKey PublicKey
        +CryptoKey PrivateKey
    }
    class HybridEcMlKemPublicKey {
        +CryptoKey EccPublicKey
        +CryptoKey MlKemPublicKey
    }
    class SymEncryptedBytes {
        +EncryptionAlgo EncryptionAlgo
        +bytes Bytes
    }
    class AesGcmBytes {
        +bytes Nonce
        +int MacLength
        +bytes BytesWithMac
    }
    class AsymEncryptedBytesWithKeyId {
        +AsymEncryptedBytes AsymEncryptedBytes
        +string KeyId
    }
    class AsymEncryptedBytes {
        +EncryptionSchema EncryptionSchema
        +bytes Bytes
    }
    class HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes {
        +bytes Derivation
        +bytes AssociatedData
        +CryptoKey SenderEcPublicKey
        +bytes MlKemEncapsulation
        +bytes Bytes
    }
    class PasswordEncryptedBytes {
        +KdfParams KdfParams
        +string Salt
        +SymEncryptedBytes Bytes
    }
    class KdfParams {
        +KdfType KdfType
        +bytes KdfParams
    }
    class KdfParamsArgon2 {
        +int Iterations
        +int MemoryPowerOf2
        +int Parallelism
        +bytes AssociatedData
    }
    class CryptoHash {
        +HashAlgo HashAlgo
        +bytes HashBytes
    }
    CryptoKeyPair *-- "2" CryptoKey
    CryptoKey ..> HybridEcMlKemPublicKey : KeyBytes when HybridEcMlKem public
    SymEncryptedBytes ..> AesGcmBytes : Bytes
    AsymEncryptedBytesWithKeyId *-- AsymEncryptedBytes
    AsymEncryptedBytes ..> HybridEcP256Mlkem768Sha256Aes256CbcSha256Bytes : Bytes when schema 3
    PasswordEncryptedBytes *-- KdfParams
    PasswordEncryptedBytes *-- SymEncryptedBytes
    KdfParams ..> KdfParamsArgon2 : KdfParams bytes
```

## Reference app wrappers

```mermaid
classDiagram
    class WrapperBase
    class Wrapper~TPayload~
    class VaultWrapper {
        +string FileName
        +GetPayloadType() "mime:application/vnd.qrs.vault"
        +GetImmutableTopLevelKey() 1
        +GenerateNewDekOnModifiedPayload false
        +AD: Title
    }
    class CommandKeyWrapper {
        +GetPayloadType() "ck"
        +GetImmutableTopLevelKey() 1
        +AD: File Title
    }
    class SecretWrapper {
        +GetPayloadType() "S"
        +AD: Website / Resource, Username, File
    }
    class FolderKeyWrapper {
        +GetPayloadType() "F"
        +GetImmutableTopLevelKey() 1
        +AD: Folder
    }
    WrapperBase <|-- Wrapper~TPayload~
    Wrapper~TPayload~ <|-- VaultWrapper : TPayload = Vault
    Wrapper~TPayload~ <|-- CommandKeyWrapper : TPayload = CryptoKey
    Wrapper~TPayload~ <|-- SecretWrapper : TPayload = Secret
    Wrapper~TPayload~ <|-- FolderKeyWrapper : TPayload = CryptoKey
```

---

Next: [Reference Applications →](12-reference-apps.md)
