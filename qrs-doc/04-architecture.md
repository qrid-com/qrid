# 4. Architecture

- [System context](#system-context)
- [Container view](#container-view)
- [Project map](#project-map)
- [Project dependency graph](#project-dependency-graph)
- [Layered view of a Client Application](#layered-view-of-a-client-application)
- [Layered view of the Mobile App](#layered-view-of-the-mobile-app)
- [Tunnel Server internals](#tunnel-server-internals)
- [Deployment topologies](#deployment-topologies)
- [Design principles](#design-principles)
- [Technology stack](#technology-stack)

---

## System context

```mermaid
flowchart TB
    User(("User"))
    subgraph Devices["User devices"]
        CA["QRS Client Applications<br/>Vault, Launcher, Password Manager,<br/>ASKPASS, your app..."]
        MA["QRS Mobile App<br/>iOS / Android"]
    end
    TS[("QRS Tunnel Server<br/>public (qrid.com) or self-hosted")]
    DNS["Domain discovery<br/>/.well-known/qrs-configuration<br/>(optional)"]
    Store[("Where the data lives<br/>local disk, USB, cloud drive, git repo...")]

    User --> CA
    User --> MA
    CA <-- "HTTPS" --> TS
    MA <-- "HTTPS" --> TS
    CA -. "HTTP GET" .-> DNS
    CA <-- "QrsEnvelope<br/>persisted objects" --> Store
```

QRS has **no backend with state**. The only network service is the Tunnel Server, which can be
replaced, restarted or scaled at any time without losing anything.

## Container view

```mermaid
flowchart LR
    subgraph CAproc["Client Application process"]
        UI1["UI layer<br/>IUserInteraction impl.<br/>(Avalonia / WinForms / Blazor / Console)"]
        APP["App logic<br/>(VaultContext, PwmContext, ...)"]
        SDK["Qrs.Client.Context<br/>ClientContext + Wrapper&lt;T&gt;"]
        TC1["Qrs.Tunnel.Client<br/>TunnelClient (Direction CA2MA)"]
        KS1[("IAsymKeyItemProvider<br/>paired MA public keys")]
        UI1 --> APP --> SDK --> TC1
        SDK --> KS1
    end

    subgraph TSproc["Qrs.Tunnel.Server process"]
        EP["Minimal API endpoints<br/>/api/QrsTunnel/*"]
        SVC["TunnelService<br/>ConcurrentDictionary of sessions<br/>+ cleanup timer"]
        EP --> SVC
    end

    subgraph MAproc["QRS Mobile App process"]
        UI2["MAUI pages<br/>Scan, Confirm, Pairing, Actions..."]
        ENG["Qrs.Mobile.Client<br/>QrsAppContext"]
        TC2["Qrs.Tunnel.Client<br/>TunnelClient (Direction MA2CA)"]
        SS[("IDevice secure storage<br/>KEK, Device Key, known servers")]
        UI2 --> ENG --> TC2
        ENG --> SS
    end

    TC1 <-- "POST /api/QrsTunnel/MessageSendPoll" --> EP
    TC2 <-- "POST /api/QrsTunnel/MessageSendPoll" --> EP
```

Note that **the same `TunnelClient` class** is used by both sides; only its `Direction` differs.
This guarantees that the encryption, AEAD binding, message numbering and other protocol details are all in sync.

## Project map

### Libraries

| Project | Target | Responsibility |
|---|---|---|
| `Qrid.Common.Proto` | net10.0 | Base Protobuf types shared by QRS and QRID: `ProtoBase`, `TypedMessage`, `StringObject`, crypto containers (`CryptoKey`, `CryptoKeyPair`, `SymEncryptedBytes`, `AsymEncryptedBytes`, `PasswordEncryptedBytes`, …) |
| `Qrid.Common` | net10.0 | **`CryptoUtils`** (all cryptography), `QrCommand` / `QrKeyValueBase` (QR string), constants (`CookieName`), utils |
| `Qrid.Common.Client` | net10.0 | `CommonApiClient` (HTTP, cookies, API key cookie), `CommonContext` (response helpers), `IUserInteraction`, cookie providers |
| `Qrid.Common.Server` | net10.0 | Server helpers (header extraction etc.) |
| `Qrs.Proto` | net10.0 | QRS Protobuf messages: `QrsEnvelope`, `IdentityData`, `AssociatedData2`, `AssociatedDataItem`, `KeyOp*`, `Pairing*`, plus partial‑class helpers; `QrsConfiguration` |
| `Qrs.Shared` | net10.0 | `QrBackupImage`, `KeyString`, discovery interfaces/DTOs |
| `Qrs.Tunnel.Shared` | net10.0 | REST DTOs (`MessageSendPollRequest/Response`, `VersionResponse`, `Directions`) and `QrsTunnelProtocol` version helpers |
| `Qrs.Client` | net10.0 | `QrsApiClient` base HTTP client |
| `Qrs.Tunnel.Client` | net10.0 | `TunnelClient` (double encryption, AEAD, numbering, long polling, QR generation, QR v2 key verification), `AsymKeyItem`, `IAsymKeyItemProvider`, `FileAsymKeyItemProvider`, `MsgHelper`, `TunnelMessageType` |
| `Qrs.Discovery.Client` | net10.0 | `DiscoveryClient` (cached domain discovery) |
| `Qrs.Client.Context` | net10.0 | **Client SDK**: `ClientContext` (command execution, pairing, switching, `WrapNewPayload`, `WrapUpdatedPayload`, `UnwrapPayload`), `WrapperBase`, `Wrapper<T>`, `RequestHelper` |
| `Qrs.Mobile.Client` | net10.0 | **MA engine**: `QrsAppContext` (KEK lifecycle, backup, `KeyOp()` execution, pairing), `QrCommandFlow` (parsing + validation), `MobUtils`, `IDevice` |
| `Qrs.UI.Avalonia` / `Qrs.UI.WinForms` | net10.0 | Built-in reusable implementations of `IUserInteraction` for Avalonia and WinForms that could be used while developing custom CA applications. |

### Applications and hosts

| Project | Kind | Description |
|---|---|---|
| `Qrs.Tunnel.Server` | ASP.NET Core Minimal API | The tunnel. Native‑AOT capable. Port 4999 by default |
| `Qrs.Mobile.App` (+ `.Droid`, `.iOS`, `.Mac`, `.WinUI`) | .NET MAUI | QRS Mobile App |
| `Qrs.App.Vault.Client` / `.Avalonia` / `.Cli` | Library / Desktop / CLI | Encrypt files and folders into `.vault` files |
| `Qrs.App.Qrs.Cli` | CLI (`qrs`) | *Launcher*: encrypted YAML → environment variables → child process |
| `Qrs.App.PasswordManager.Client` / `.Components` / `.Hybrid` | Library / Razor components / MAUI Blazor Hybrid | Password manager |
| `Qrs.App.SshAskPass.Cli` / `Qrs.App.SudoAskPass.Cli` | Avalonia / CLI | `SSH_ASKPASS`, `SUDO_ASKPASS` helpers - work in progress - need some testing / usage |
| `Qrs.App.FileManager.*`, `Qrs.App.EmailTools.Avalonia`, `Qrs.App.Analytics.*` | various | Experimental / internal tools |
| `Qrs.TestConsole` | Console | Headless end‑to‑end integration test |

## Project dependency graph

```mermaid
flowchart BT
    CP[Qrid.Common.Proto]
    QP[Qrid.Proto]
    C[Qrid.Common]
    CC[Qrid.Common.Client]
    CS[Qrid.Common.Server]

    RP[Qrs.Proto]
    RS[Qrs.Shared]
    TSh[Qrs.Tunnel.Shared]
    RC[Qrs.Client]
    TCl[Qrs.Tunnel.Client]
    DC[Qrs.Discovery.Client]
    CTX[Qrs.Client.Context]
    MC[Qrs.Mobile.Client]
    MAPP[Qrs.Mobile.App]
    TSrv[Qrs.Tunnel.Server]
    UIA[Qrs.UI.Avalonia]
    VC[Qrs.App.Vault.Client]
    PWM[Qrs.App.PasswordManager.Client]
    CLI[Qrs.App.Qrs.Cli]
    ASK[Qrs.App.SshAskPass.Cli]
    TEST[Qrs.TestConsole]

    C --> CP
    C --> QP
    CC --> C
    RP --> CP
    RS --> RP
    RS --> C
    TSh --> CP
    RC --> CC
    TCl --> RC
    TCl --> RP
    TCl --> RS
    TCl --> TSh
    DC --> RS
    DC --> CC
    CTX --> TCl
    CTX --> DC
    MC --> CTX
    MAPP --> MC
    TSrv --> TSh
    TSrv --> C
    TSrv --> CS
    UIA --> CTX
    VC --> CTX
    PWM --> CTX
    CLI --> CTX
    ASK --> CTX
    TEST --> MC
    TEST --> VC
    TEST --> PWM
```

Arrows point from a project to what it references. 

## Layered view of a Client Application

```mermaid
flowchart TB
    subgraph L4["Presentation"]
        A1["Your UI (Avalonia, WinForms, Blazor, Spectre.Console)"]
        A2["IUserInteraction implementation<br/>ShowQrCodeWindowAsync, HideQrCodeWindow,<br/>GetUserInputAsync('pairing-code'), SetTunnelServiceUri"]
    end
    subgraph L3["Application"]
        B1["YourContext : ClientContext"]
        B2["YourWrapper : Wrapper&lt;TPayload&gt;<br/>GetPayloadType, GetAssociatedData2,<br/>GetImmutableTopLevelKey, GetImmutableDekId"]
    end
    subgraph L2["QRS SDK"]
        C1["ClientContext<br/>WrapNewPayload, WrapUpdatedPayload, UnwrapPayload,<br/>KeyOpAsync, ExecuteQrCommandAsync, PairingFlowAsync"]
        C2["TunnelClient<br/>EncryptSendPollDecrypt, GetQrString"]
        C3["IAsymKeyItemProvider<br/>(FileAsymKeyItemProvider)"]
        C4["DiscoveryClient"]
    end
    subgraph L1["Foundation"]
        D1["CryptoUtils (BouncyCastle)"]
        D2["Protobuf messages"]
        D3["CommonApiClient (HttpClient + cookies)"]
    end
    L4 --> L3 --> L2 --> L1
```

## Layered view of the Mobile App

```mermaid
flowchart TB
    subgraph P["Qrs.Mobile.App (MAUI)"]
        P1["Account/*: Welcome, NewKey, ExistingKey, CreateBackup"]
        P2["Main/*: MainPage, ScanPage, ScanInputPage,<br/>PairingPage, PairingConfirm, ScanConfirm,<br/>ActionsPage, SettingsPage"]
        P3["Controls/*: HowItWorks, AlertsView,<br/>CommandDataView, CommandErrorsView"]
    end
    subgraph E["Qrs.Mobile.Client"]
        E1["QrsAppContext<br/>NewMasterKeyAsync, ExistingMasterKeyAsync,<br/>BackupMasterKeyAsync, DownloadRequestAsync,<br/>KeyOp, QrCommandSendResponseAsync, ResetPairing"]
        E2["Flows: NewUserFlow, ExistingUserFlow,<br/>BackupFlow, QrCommandFlow"]
        E3["MobUtils + IDevice<br/>(secure storage)"]
    end
    subgraph F["Shared"]
        F1["TunnelClient (MA2CA)"]
        F2["CryptoUtils"]
    end
    P --> E --> F
```

## Tunnel Server internals

```mermaid
flowchart TB
    REQ["POST /api/QrsTunnel/MessageSendPoll"] --> V1{"Direction valid?<br/>size <= 200,000 chars?"}
    V1 -- no --> ERR1["ERROR response"]
    V1 -- yes --> GS["GetOrCreateSession(TunnelHashId)"]
    GS --> EX{exists?}
    EX -- yes --> S["TunnelSessionItem"]
    EX -- no --> AK{"API key required<br/>and invalid?"}
    AK -- yes --> ERR2["ERROR: valid API key required"]
    AK -- no --> CAP{"sessions >= MaxSessionCount?"}
    CAP -- yes --> ERR3["ERROR: overloaded"]
    CAP -- no --> NEW["create session + enqueue for expiry"] --> S
    S --> SEND{"Message != null?"}
    SEND -- yes --> NUM{"MessageNumber > last<br/>in sending direction?"}
    NUM -- no --> ERR4["ERROR: not greater"]
    NUM -- yes --> STORE["store latest message,<br/>complete waiting pollers"]
    STORE --> POLL
    SEND -- no --> POLL{"ExpectResponse?"}
    POLL -- no --> OK1["SUCCESS, MessageNumber=-1"]
    POLL -- yes --> AV{"receiving.MessageNumber<br/>>= ExpectedResponseNumber?"}
    AV -- yes --> OK2["SUCCESS with message"]
    AV -- no --> TO{"PollingTimeout > 0?"}
    TO -- no --> T1["TIMEOUT, MessageNumber=-2"]
    TO -- yes --> WAIT["await TaskCompletionSource<br/>or Task.Delay(PollingTimeout)"]
    WAIT --> OK3["SUCCESS with message"]
    WAIT --> T2["TIMEOUT, MessageNumber=-3"]
```

A background `System.Timers.Timer` runs every **10 s** and removes sessions older than
**6 minutes** (FIFO queue), cancelling any pending pollers. See
[Tunnel Server Operations](13-tunnel-server-ops.md) for configuration and monitoring endpoints.

## Deployment topologies

### A. Public tunnel (default)

```mermaid
flowchart LR
    CA["Client Apps<br/>(anywhere)"] -- HTTPS --> LB["qrid.com<br/>reverse proxy / rate limiting"] --> T["Qrs.Tunnel.Server"]
    MA["Phones<br/>(mobile network)"] -- HTTPS --> LB
```

### B. Self‑hosted on Google Cloud Run (scale to zero)

```mermaid
flowchart LR
    CA["Client Apps<br/>Settings: TunnelServerUri"] -- HTTPS --> CR["Cloud Run service<br/>Native AOT image<br/>min-instances=0"]
    MA["Phones"] -- HTTPS --> CR
```

> **Scaling note:** sessions are held **in memory of a single instance**. When running more than one
> instance, both the CA and the MA of the same session must reach the same instance. Keep
> `max-instances=1` or use session affinity keyed on the Tunnel Id. See
> [Tunnel Server Operations](13-tunnel-server-ops.md#scaling).

### C. LAN / air‑gapped office

```mermaid
flowchart LR
    CA["Workstations"] -- "http://10.0.0.5:4999" --> T["Qrs.Tunnel.Server<br/>(Docker on a LAN host)"]
    MA["Phones on office Wi-Fi"] -- "http://10.0.0.5:4999" --> T
```

Plain HTTP on a trusted LAN is acceptable because the two application‑level encryption layers
remain; the MA shows an explicit alert when a tunnel URL uses plain HTTP. `ClientContext.InitDomain`
also accepts a `%localip%` placeholder in the tunnel URL template for developer machines.

## Design principles

1. **Keys on the phone, data with the app, nothing on the server.**
2. **Zero‑knowledge relay.** The server stores only the latest opaque message per direction, for
   minutes, in RAM.
3. **Out‑of‑band bootstrap.** Session secrets go through the camera, not the network.
4. **Human in the loop.** Every KEK use requires an explicit, informed confirmation.
5. **Bind everything.** Direction, message number, protocol version, application name, DEK
   identity and human metadata are all AEAD‑bound.
6. **Generic Mobile App.** New Client Applications must not require MA changes: they express
   themselves through `KeyOp` + `AssociatedData2`.
7. **Symmetric code paths.** CA and MA share `TunnelClient` and `CryptoUtils`.
8. **Crypto in one place.** All primitives live in `Qrid.Common/Crypto/CryptoUtils.cs`.
9. **Protobuf for payloads, JSON only for the tunnel envelope.**
10. **Versioned protocols** with SemVer and "please upgrade" errors instead of silent failures.

## Technology stack

| Area | Technology |
|---|---|
| Language / runtime | C# on .NET 10 |
| Tunnel server | ASP.NET Core Minimal API, `System.Text.Json` source generation, Native AOT (`PublishAot=true` in `Dockerfile.gcr`), Serilog |
| Mobile App | .NET MAUI (Android, iOS; Mac Catalyst and WinUI heads for development) |
| Desktop UI | Avalonia, WinForms, MAUI Blazor Hybrid |
| CLI | Spectre.Console, QRCoder (ASCII QR codes) |
| Crypto | BouncyCastle (AES‑GCM, ML‑KEM, ECDH, HKDF, SHA‑3, Argon2) |
| Serialization | Protocol Buffers (Google.Protobuf), JSON for the tunnel REST envelope |
| Containers | Docker (`Dockerfile`, `Dockerfile.gcr`), Docker Compose (`qridroot/intest/compose.yml`), Google Cloud Build (`cloudbuild.yaml`) |
| CI | GitHub Actions `QRS Integration Test` (`.github/workflows/qrs-intest.yml`) |

---

Next: [Cryptography →](05-cryptography.md)
