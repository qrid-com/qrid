# QRS — Post‑Quantum, Password‑less End‑to‑End Encryption via QR Codes

> **Scan a QR code instead of typing a password.**
> QRS lets any Desktop, Web, CLI or Mobile application encrypt user data on the client side with
> random AES‑256 keys that are wrapped by a Master Key that never leaves the user's phone —
> transported over a zero‑knowledge tunnel protected by hybrid **ML‑KEM‑768 + ECDH P‑256**
> post‑quantum cryptography.

This is the developer documentation for the **QRS** platform in the
`qrid-com/QRID` repository.

> **Status:** QRS is in **public beta**. The protocols described here are versioned
> (`QRS Client Protocol 1.0.0`, `QRS Server Protocol 1.0.0`) and implemented by the code in the
> `Qrs.*` projects of this repository.

---

## How it works

![How it works](qrs-doc\img\Diagram-Fig-1.svg)




1. Your application shows a QR code.
2. The user scans it with the **QRS Mobile App** and confirms the operation on the phone.
3. The phone's **Master Key (KEK)** wraps or unwraps a **Data Encryption Key (DEK)** and sends it
   back to your application through an end‑to‑end encrypted tunnel (QR code has tunnel info and keys) .
4. Your application encrypts/decrypts the data locally with the DEK. **Nothing** is stored on any
   server.

---

## Documentation map

| # | Document | Audience | What you will learn |
|---|---|---|---|
| 1 | [Why QRS?](qrs-doc/01-why-qrs.md) | Everyone | The problem, comparisons with passwords, password managers, hardware keys and cloud KMS |
| 2 | [Core Concepts & Glossary](qrs-doc/02-concepts.md) | Everyone | KEK, DEK, CA, MA, Tunnel, Associated Data, Identity Data, Envelope, Pairing… |
| 3 | [How It Works](qrs-doc/03-how-it-works.md) | Everyone | End‑user journeys and the high‑level data flow |
| 4 | [Architecture](qrs-doc/04-architecture.md) | Developers, operators | Components, project map, dependency graph, deployment topologies |
| 5 | [Cryptography](qrs-doc/05-cryptography.md) | Security reviewers, developers | Every algorithm used by QRS |
| 6 | [Tunnel Protocol Specification](qrs-doc/06-tunnel-protocol.md) | Protocol implementers | QR code format, REST API, message numbering, long polling, versioning |
| 7 | [Key Operations & Envelope Format](qrs-doc/07-key-operations.md) | App developers | `KeyOp` API, `QrsEnvelope`, wrap / unwrap / reseal flows |
| 8 | [Pairing & Key Switching](qrs-doc/08-pairing.md) | Protocol implementers | How a CA learns the MA's public key |
| 9 | [QRS Mobile App Internals](qrs-doc/09-mobile-app.md) | MA contributors | Screens, flows, secure storage, validation rules |
| 10 | [Client SDK Guide — Build Your Own QRS App](qrs-doc/10-client-sdk.md) | App developers | Step‑by‑step tutorial with C# code |
| 11 | [Class Diagrams](qrs-doc/11-class-diagrams.md) | Contributors | UML class diagrams of every QRS library |
| 12 | [Reference Applications](qrs-doc/12-reference-apps.md) | Everyone | Vault, Launcher, Password Manager, SSH/SUDO ASKPASS |
| 13 | [Tunnel Server Operations](qrs-doc/13-tunnel-server-ops.md) | Operators | Self‑hosting, Docker, Cloud Run, configuration, monitoring |
| 14 | [Building & Testing](qrs-doc/14-building-and-testing.md) | Contributors | Prerequisites, solutions, integration tests, protobuf generation |
| 15 | [Security Model & Threat Analysis](qrs-doc/15-security-model.md) | Security reviewers | Trust boundaries, attacker models, guarantees, known limitations |
| 16 | [Porting Guide — QRS in Other Languages](qrs-doc/16-porting-guide.md) | SDK authors | How to port QRS protocols to another programming language to implement a compatible CA or MA |
| 17 | [FAQ](qrs-doc/17-faq.md) | Everyone | Frequently asked questions |
| — | [Roadmap](ROADMAP.md) | Everyone | Where the project is heading |
| — | [Contributing](CONTRIBUTING.md) | Contributors | How to propose changes |
| — | [Security Policy](SECURITY.md) | Security researchers | How to report vulnerabilities |
| — | [Code of Conduct](CODE_OF_CONDUCT.md) | Everyone | Community standards |
| — | [Changelog](CHANGELOG.md) | Everyone | Protocol and component versions |

## Key facts at a glance

| Property | Value |
|---|---|
| Master Key (KEK) | Random AES‑256 key, generated and stored only on the phone |
| Data Encryption Key (DEK) | Random AES‑256 key, generated on the phone, returned to the CA, stored **wrapped** next to the data |
| Key wrapping | AES‑256‑GCM with Associated Data (AD) + Identity Data (ID) bound as AEAD |
| Tunnel encryption | Two application layers — hybrid PQC (ML‑KEM‑768 + ECDH P‑256 → HKDF‑SHA‑256 → AES‑256‑GCM) inside AES‑256‑GCM session encryption - plus network layer i.e. TLS provided by your operating system - totals to 3 layers of encryption|
| Master Key backup | Password‑encrypted QR "Backup Image" (Argon2id KDF → AES‑256‑GCM, key name bound as AD) |
| Server‑side state | None persisted. Tunnel sessions live in RAM for ~6 minutes at most |
| Accounts | None. No usernames, no e‑mail, no cloud accounts |
| Transport | HTTPS REST + JSON envelope, Protobuf payloads |
| Stack | C# / .NET 10, ASP.NET Core Minimal API (Native AOT capable), .NET MAUI, Avalonia, BouncyCastle |
| Platforms | MA: iOS, Android (plus macOS / Windows heads). CA: Windows, macOS, Linux, CLI, Web (Blazor) |
| License | MIT (see [FAQ](qrs-doc/17-faq.md#what-is-the-license)) |

---

## Repository layout (QRS part only)

```text
Qrs.Tunnel.Server/        Zero-knowledge tunnel (ASP.NET Core Minimal API, Native-AOT ready)
Qrs.Tunnel.Shared/        Tunnel REST DTOs + protocol version helpers
Qrs.Tunnel.Client/        Tunnel client used by BOTH CA and MA (double encryption, AEAD, numbering)
Qrs.Proto/                Protobuf messages: QrsEnvelope, KeyOp*, Pairing*, AssociatedData*, IdentityData
Qrs.Shared/               Shared helpers (discovery DTOs, QR backup image, key strings)
Qrs.Client/               Base API client for QRS
Qrs.Client.Context/       Client SDK: ClientContext + Wrapper<T> (what app developers use)
Qrs.Discovery.Client/     Domain discovery (/.well-known/qrs-configuration)
Qrs.Mobile.Client/        Mobile-side engine: QrsAppContext (KEK, KeyOp execution, pairing)
Qrs.Mobile.App/           .NET MAUI Mobile App (+ Droid / iOS / Mac / WinUI heads)
Qrs.UI.Avalonia/          Reusable QR-code window + input dialog (Avalonia)
Qrs.UI.WinForms/          Reusable QR-code window + input dialog (WinForms)
Qrs.App.*/                Reference Client Applications (Vault, Launcher CLI, Password Manager, ASKPASS...)
Qrs.TestConsole/          Headless end-to-end integration test (real tunnel, simulated MA + CA)
Qrid.Common*/             Crypto (CryptoUtils), HTTP, Protobuf base types shared with QRID
QRSHL.sln                 QRS solution - headless - to run builds in any OS including GitHub Agents
QRS.sln                   QRS solution - to open in IDE for local development, includes everything
run-qrs-intest.ps1        One-command integration test (Docker)
```

---

## Quick start (5 minutes)

```bash
# Run the full headless end-to-end test (builds and starts a local tunnel server in Docker,
# simulates the Mobile App and a Vault Client App, then tears everything down)
pwsh ./run-qrs-intest.ps1
```

Then continue with the [Client SDK Guide](qrs-doc/10-client-sdk.md) to integrate QRS in your own application.

---

## Credits & contact

QRS is developed by the QRID team (<https://qrid.com>). Questions, ideas or interest in client
libraries for other languages: **info@qrid.com**.

Next: [Why use QRS? →](qrs-doc/01-why-qrs.md)
