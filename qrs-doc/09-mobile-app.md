# 9. Mobile App Internals

The **QRS Mobile App (MA)** is the user's hardware‑like key: it holds the Master Key (KEK) and the
Device Key, scans QR codes, shows what is being requested and performs key operations after the
user approves.

- [Projects](#projects)
- [Layering](#layering)
- [Secure storage](#secure-storage)
- [Screens and navigation](#screens-and-navigation)
- [Account flows](#account-flows)
- [Command flow (scan → approve → respond)](#command-flow-scan--approve--respond)
- [Alerts and validation](#alerts-and-validation)
- [Actions and settings](#actions-and-settings)
- [Threading notes](#threading-notes)
- [Building and running the MA](#building-and-running-the-ma)

---

## Projects

| Project | Target | Role |
|---|---|---|
| `Qrs.Mobile.App/Qrs.Mobile.App` | `net10.0`, .NET MAUI | Shared UI (XAML pages, controls, navigation) |
| `Qrs.Mobile.App/Qrs.Mobile.App.Droid` | Android head | Platform entry point |
| `Qrs.Mobile.App/Qrs.Mobile.App.iOS` | iOS head | Platform entry point |
| `Qrs.Mobile.App/Qrs.Mobile.App.Mac` | Mac Catalyst head | Platform entry point |
| `Qrs.Mobile.App/Qrs.Mobile.App.WinUI` | Windows head | Platform entry point (development/testing) |
| `Qrs.Mobile.Client` | `net10.0` | **UI‑independent engine**: `QrsAppContext`, flows, validation, key ops, secure‑storage helpers. Also driven headless by `Qrs.TestConsole`. |

Key UI packages: `CommunityToolkit.Maui`, `BarcodeScanning.Native.Maui` (QR scanning),
`QRCoder` (Backup Image rendering).

## Layering

```mermaid
flowchart TB
    subgraph UI["Qrs.Mobile.App (MAUI)"]
        Pages["Pages: Welcome, NewKey/*, ExistingKey/*, CreateBackup/*,<br/>MainPage, ScanPage, ScanInputPage, ScanConfirm,<br/>PairingPage, PairingConfirm, ActionsPage, SettingsPage"]
        Controls["Controls: HowItWorks, AlertsView, CommandDataView,<br/>CommandErrorsView, TextInput, TitleView"]
        MD["MobileDevice : IDevice<br/>(MAUI SecureStorage)"]
    end
    subgraph Engine["Qrs.Mobile.Client"]
        Ctx["QrsAppContext (partial)<br/>NewUser, ExistingUser, KeyBackup,<br/>QrCommand, KeyOperations"]
        Flows["NewUserFlow, ExistingUserFlow,<br/>BackupFlow, QrCommandFlow"]
        Data["OrangeKeys, RequestItem"]
        MU["MobUtils (secure storage helpers)"]
    end
    subgraph Libs["Shared libraries"]
        TC["Qrs.Tunnel.Client (TunnelClient)"]
        QP["Qrs.Proto"]
        CU["Qrid.Common (CryptoUtils)"]
    end
    Pages --> Ctx
    Controls --> Ctx
    Ctx --> Flows
    Ctx --> Data
    Ctx --> MU
    MU --> MD
    Ctx --> TC
    Ctx --> QP
    Ctx --> CU
    TC --> CU
```

## Secure storage

All secrets are stored through `IDevice` → MAUI `SecureStorage` (iOS Keychain, Android
Keystore‑encrypted preferences, Windows DPAPI).

| Key | Value | Written by |
|---|---|---|
| `MasterKeySym` | base64 `CryptoKey` (AES‑256 KEK) | New key / restore |
| `MasterKeyName` | user‑chosen key name | New key / restore |
| `DeviceKeyItem` | base64 hybrid `CryptoKeyPair` | First use / Reset Pairing |
| `KnownTunnelServers` | JSON array of tunnel URLs the user accepted | Approve with a new server |

**Logout** calls `SecureStorage.RemoveAll()` — Master Key, Device Key and known servers are all
wiped. Only a Backup Image + password can restore the Master Key afterwards.

## Screens and navigation

```mermaid
flowchart TD
    Start(["App start"]) --> Has{"Master Key in secure storage?"}
    Has -- no --> W["Account/Welcome<br/>(HowItWorks)"]
    W -- "Create new key" --> NK1["NewKey/Password<br/>key name + backup password"]
    NK1 --> NK2["NewKey/BackupImage<br/>QR backup to save/print"]
    NK2 --> NK3["NewKey/Congratulations"]
    W -- "I have a backup" --> EK1["ExistingKey/BackupScan<br/>scan Backup Image"]
    EK1 --> EK2["ExistingKey/BackupPassword"]
    EK2 --> EK3["ExistingKey/Congratulations"]
    NK3 --> M
    EK3 --> M
    Has -- yes --> M["Main/MainPage<br/>Loaded key, Scan, Actions"]
    M -- Scan --> S["Main/ScanPage (camera)"]
    M -- "Actions" --> A["Main/ActionsPage"]
    A -- "Enter Qr Code String" --> SI["Main/ScanInputPage"]
    A -- "Backup Master Key" --> B1["CreateBackup/BackupPassword"]
    B1 --> B2["CreateBackup/BackupImage"] --> B3["CreateBackup/Congratulations"] --> M
    A -- Settings --> ST["Main/SettingsPage"]
    A -- Logout --> W
    S --> D{"Payload or pairing?"}
    SI --> D
    D -- pairing --> P1["Main/PairingPage"] --> P2["Main/PairingConfirm<br/>shows 3-letter code"]
    P2 -- Continue --> SC
    D -- payload --> SC["Main/ScanConfirm<br/>alerts, errors, request items<br/>YES / NO"]
    SC -- "YES / NO" --> M
```

## Account flows

### Create a new Master Key

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant UI as NewKey pages
    participant C as QrsAppContext
    participant SS as SecureStorage
    U->>UI: key name + backup password
    UI->>C: NewMasterKeyAsync(password, keyName)
    C->>C: KEK = random AES-256
    C->>C: backup = PasswordEncryptCryptoKey(KEK, password, AD = StringObject(keyName))<br/>Argon2id + AES-256-GCM
    C->>SS: MasterKeySym, MasterKeyName
    UI->>U: Backup Image QR "key=...&name=..." (save / print)
```

### Restore from a Backup Image

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant UI as ExistingKey pages
    participant C as QrsAppContext
    participant SS as SecureStorage
    U->>UI: scan Backup Image
    UI->>C: ParseBackupImage(text): key + name (legacy: raw key only)
    U->>UI: password
    UI->>C: ExistingMasterKeyAsync(password, key, name)
    C->>C: PasswordDecryptCryptoKey(peb, password, AD = StringObject(name))
    alt wrong password or tampered name
        C-->>UI: false, show error
    else success
        C->>SS: MasterKeySym, MasterKeyName
        C-->>UI: true
    end
```

### Backup the current key

`BackupMasterKeyAsync(password)` re‑encrypts the loaded KEK with a (possibly new) password and
renders the Backup Image. Each backup uses a fresh random salt and nonce.

## Command flow (scan → approve → respond)

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant SP as ScanPage
    participant C as QrsAppContext
    participant F as QrCommandFlow
    participant TC as TunnelClient
    participant SC as ScanConfirm

    U->>SP: point camera at QR
    SP->>C: DownloadRequestAsync(QrCommand)
    C->>C: PayloadOrPairing(): ValidateTunnelUrl, ValidateQrCommandProtocolVersion
    C->>F: Alerts (plain HTTP, new server)
    C->>TC: InitMobileApplication(qc, DeviceKeyItem), PollDecrypt(2000)
    TC-->>C: MsgHelper (payload / switch / pairing)
    C->>F: Payload, ParsePayload(KekName), ValidateCommand(KekName)
    C-->>SC: DownloadCompleted()
    SC->>U: alerts, validation errors, items (op + AD)
    alt user taps YES and no fatal errors
        SC->>C: add pending known server, KeyOp()
        C->>TC: EncryptSend(KeyOpResponse)
    else user taps NO
        SC->>SC: nothing is sent, CA times out
    end
```

`ParsePayload` turns the `KeyOpRequest` into a list of `RequestItem` objects (`IdentityData`,
`OldAssociatedData`, `NewAssociatedData`, `DisplayAssociatedData`); `KeyOp()` then produces a
`KeyOpResponse` (see [Key Operations](07-key-operations.md#operation-details)).

## Alerts and validation

| Category | Examples | Effect |
|---|---|---|
| **Alerts** (informational, shown in `AlertsView`) | Plain HTTP tunnel; first use of a non‑`*.qrid.com` server | User decides; new server is remembered after **YES** |
| **Validation errors** | Multiple app names; non‑canonical app name | Block **YES** unless *Ignore Validation Errors (for this session only)* is enabled in Settings |
| **Fatal errors** | Empty request; multiple/missing KEK names; KEK not loaded; item parse error | Block **YES** |

## Actions and settings

| Action | Effect |
|---|---|
| Backup Master Key | Create a new Backup Image (new password allowed). |
| Logout | Wipe secure storage (KEK, Device Key, known servers). |
| Settings | *Ignore Validation Errors (for this session only)*. |
| Enter Qr Code String | Paste a QR string instead of scanning (desktop testing, accessibility). |
| Reset Pairing | Generate a new Device Key; all CAs must re‑pair. |
| Reset Known Servers | Forget accepted tunnel servers; alerts will reappear. |
| Test | Developer diagnostics. |

## Threading notes

- `QrsAppContext.InitAsync` runs initialisation on a background thread so iOS' launch watchdog is
  not triggered by secure‑storage access or key generation.
- Network and crypto calls from pages are wrapped in `Task.Run` with a loading overlay
  (`IsLoading`).
- `MobileDevice` exposes both sync and async secure‑storage methods; the sync versions block on a
  background task and must not be called from the UI thread in new code.

## Building and running the MA

See [Building & Testing](14-building-and-testing.md#mobile-app). In short: install the .NET 10 SDK
and the MAUI workload (`dotnet workload install maui`), then build the platform head you need,
e.g. `dotnet build Qrs.Mobile.App/Qrs.Mobile.App.Droid -c Debug`. For end‑to‑end tests without a
phone, `Qrs.TestConsole` drives `Qrs.Mobile.Client` directly.

> **Release builds only for real users.** Debug builds use weak Argon2 parameters for backups and
> force long polling timeouts.

---

Next: [Client SDK Guide →](10-client-sdk.md)
