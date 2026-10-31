# 3. How It Works

This page explains QRS from the **user's** point of view first, then opens the hood one level to
show what travels where. Protocol‑level detail is in the
[Tunnel Protocol](06-tunnel-protocol.md), [Key Operations](07-key-operations.md) and
[Pairing](08-pairing.md) specifications.

- [1. The user journey](#1-the-user-journey)
- [2. First‑time setup on the phone](#2-firsttime-setup-on-the-phone)
- [3. Encrypting data (wrap)](#3-encrypting-data-wrap)
- [4. Decrypting data (unwrap)](#4-decrypting-data-unwrap)
- [5. First use of a new Client Application (pairing)](#5-first-use-of-a-new-client-application-pairing)
- [6. Moving to a new phone (backup & restore)](#6-moving-to-a-new-phone-backup--restore)
- [7. What is inside the QR code?](#7-what-is-inside-the-qr-code)
- [8. Example data flow](#8-the-big-picture-data-flow)
- [9. What each party knows](#9-what-each-party-knows)

---

## 1. The user journey

```mermaid
journey
    title Using QRS app
    section Once
      Install QRS Mobile App: 5: User
      Create Master Key: 4: User
      Backup Master Key to Backup Image + password: 3: User
    section First time using an app
      App shows QR code: 5: User, CA
      Scan, type 3-letter pairing code in the app: 3: User, CA, MA
      Confirm operation on phone: 5: User, MA
    section Every day
      App shows QR code: 5: User, CA
      Scan and tap Yes: 5: User, MA
      Data is encrypted or decrypted: 5: CA
```

## 2. First‑time setup on the phone

```mermaid
flowchart TD
    A([Open QRS Mobile App]) --> B{Master Key<br/>already loaded?}
    B -- yes --> S
    B -- no --> W[Welcome page]
    W --> N[New Master Key]
    W --> E[Load Existing Master Key]

    N --> N1[Enter key name + backup password]
    N1 --> N2["MA generates random AES-256<br/> Master Key and encrypts it with Argon2id(password)<br/>AD = key name"]
    N2 --> N3[Backup Image QR shown<br/>save / print / screenshot it]
    N3 --> N4[Congratulations] --> M

    E --> E1[Scan Backup Image QR]
    E1 --> E2[Enter backup password]
    E2 --> E3{Decrypts OK?}
    E3 -- no --> E2
    E3 -- yes --> E4[Master Key saved to secure storage] --> M

    M[Main Page] -- Scan Button --> S[Scaner Page]
    S -- Scan QR<br/>Download Request<br/>Decrypt Request  --> C[Confirm Y/N]
    C -- Perform Crypto Operation<br/>using Master Key:<br/>WRAP DEK / UNWRAP DEK / ETC<br/>Send Response  --> Done

```

The Master Key is a **random** 256‑bit key — the backup password only protects the *backup copy*.
Losing the phone without a Backup Image means losing access to the data.

## 3. Encrypting data (wrap)

Example: the user saves a new file into **QRS Vault**.

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant CA as Client App (QRS Vault)
    participant T as Tunnel Server
    participant MA as QRS Mobile App

    U->>CA: Save "taxes.vault"
    CA->>CA: New session: random Session Key + ephemeral PQC key pair
    CA->>T: POST KeyOpRequest(WrapNewPayload, AD Title=taxes.vault) [encrypted]
    CA-->>U: Show QR code (tunnel URL + Session Key + key hash)
    U->>MA: Scan QR code
    MA->>T: Poll for request
    T-->>MA: Encrypted KeyOpRequest
    MA->>MA: Decrypt, validate, show "QRS VAULT - Title: taxes.vault"
    U->>MA: Tap "Yes"
    MA->>MA: Generate random DEK, build IdentityData, wrap DEK with KEK
    MA->>T: KeyOpResponse(DEK, wrapped DEK, AD, ID) [encrypted]
    T-->>CA: Encrypted KeyOpResponse (long-poll completes)
    CA->>CA: Encrypt file with DEK, write QrsEnvelope to taxes.vault
    CA-->>U: Saved
```

## 4. Decrypting data (unwrap)

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant CA as Client App
    participant T as Tunnel Server
    participant MA as QRS Mobile App

    U->>CA: Open "taxes.vault"
    CA->>CA: Read QrsEnvelope, recompute AD from real data, compare with envelope
    CA->>T: KeyOpRequest(Unwrap, wrapped DEK, AD, ID) [encrypted]
    CA-->>U: Show QR code
    U->>MA: Scan QR
    MA->>T: Poll
    T-->>MA: Encrypted request
    MA-->>U: "QRS VAULT wants to UNWRAP - Title: taxes.vault - Master Key: MyKey"
    U->>MA: Tap "Yes"
    MA->>MA: AES-GCM decrypt wrapped DEK with KEK (AAD = AD || ID)
    MA->>T: KeyOpResponse(DEK) [encrypted]
    T-->>CA: Encrypted response
    CA->>CA: Decrypt payload with DEK (AAD = ID)
    CA-->>U: File contents
```

If the user taps **No**, the MA simply sends nothing; the CA's long poll times out and reports
*"Timeout - no response from QRS mobile application"*.

## 5. First use of a new Client Application (pairing)

The first time a CA talks to an MA it does not know the MA's public key. It asks the MA to pair.

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant CA as Client App
    participant T as Tunnel
    participant MA as Mobile App

    CA->>T: Pairing-Requested-CA [session-key layer only]
    CA-->>U: QR code
    U->>MA: Scan
    MA->>T: Poll
    T-->>MA: Pairing-Requested-CA
    MA->>MA: Pick random 3-letter code (e.g. "kqz")
    MA->>T: Pairing-Requested-MA {Device PubKey, KeyId, code} [double-encrypted to CA]
    MA-->>U: Shows "kqz"
    T-->>CA: Pairing-Requested-MA
    CA-->>U: "Enter pairing code"
    U->>CA: types "kqz"
    CA->>CA: Code matches: save MA public key (paired)
    CA->>T: KeyOpRequest [double-encrypted to MA Device key]
    U->>MA: Tap "Continue"
    MA->>T: Poll
    T-->>MA: KeyOpRequest
    Note over U,MA: Continues exactly like a normal wrap/unwrap (confirm screen)
```

Details, including the *switch key* optimisation and the state machines, are in
[Pairing & Key Switching](08-pairing.md).

## 6. Moving to a new phone (backup & restore)

```mermaid
flowchart LR
    subgraph Old["Old phone"]
        K1[KEK] --> B1["Actions, Backup Master Key<br/>+ password"]
    end
    B1 --> IMG[/"Backup Image<br/>(QR: key=...&name=...)"/]
    IMG --> S1["print / store offline<br/>in several places"]
    subgraph New["New phone (any OS)"]
        L1["Load Existing Master Key"] --> L2[scan Backup Image] --> L3[enter password] --> K2[KEK restored]
    end
    IMG --> L2
```

Because every CA stores its DEKs *wrapped* next to the data, restoring the KEK instantly restores
access to **all** data in **all** QRS applications. A new phone gets a **new Device Key**, so each
CA will transparently pair again on its first use.

## 7. What is inside the QR code?

```text
v=2&cmd=keyop&srv=https%3A%2F%2Fqrid.qrid.com%2Fqrs-tunnel-api%2F&cph=<43 chars>&sk=<Session Key>&cpv=1.0.0
```

| Param | Meaning | Secret? |
|---|---|---|
| `v` | QR format version (`2`) | no |
| `cmd` | Command, currently `keyop` | no |
| `srv` | Tunnel Server base URL | no |
| `cph` | Tunnel Id = SHA3‑256 hash of the CA's ephemeral public key | no, but authenticates the CA key |
| `sk` | **Session Key** — AES‑256 key protecting every tunnel message | **yes** |
| `cpv` | CA's QRS Client Protocol version | no |

The QR code is the *out‑of‑band channel*: it is seen by the phone camera only, never by the
tunnel server. Anyone who can photograph the screen during those seconds gets the session key, but
still cannot read requests to the MA (they are also encrypted to the MA's Device public key) or response to MA (also encrypted with CA's ephemeral public key).

## 8. Example data flow - Client Application decrypts QRS-encrypted data

```mermaid
flowchart TB
    subgraph CA["Client Application (e.g. laptop)"]
        direction TB
        P["QRS-encrypted data"] -- deserialize --> ENV[("QrsEnvelope<br/>object:<br/>-Wrapped DEK<br/>-Payload encrypted with DEK")]
        ENV -- "wrapped DEK + AD + ID" --> REQ["KeyOpRequest"]
        REQ --> L1["Layer A: Hybrid PQC to MA Device PubKey"]
        L1 --> L2["Layer B: AES-256-GCM Session Key<br/>AAD = 'CA2MA:n:1.0.0'"]
    end

    L2 -- "Layer C: TLS" --> TS[("Tunnel Server<br/>RAM mailbox per Tunnel Id")]
    TS -- "TLS" --> M2

    subgraph MA["QRS Mobile App (phone)"]
        direction TB
        M2["Remove Layer B (Session Key from QR)"] --> M1["Remove Layer A (Device PrivKey)"]
        M1 --> SHOW["Show App Name + AD<br/>user taps Yes"]
        SHOW --> OP["KEK: unwrap / wrap DEK<br/>AES-256-GCM, AAD = AD || ID"]
        OP --> R1["Layer A: Hybrid PQC to CA ephemeral PubKey"]
        R1 --> R2["Layer B: Session Key<br/>AAD = 'MA2CA:n:1.0.0'"]
    end

    R2 -- "TLS" --> TS
    TS -- "TLS (long-poll response)" --> BACK["CA removes B, then A,<br/>gets DEK, decrypts data"]
```

## 9. What each party knows

| Secret / data | User | CA | Tunnel Server | MA | Network attacker |
|---|---|---|---|---|---|
| Master Key (KEK) | – | ❌ | ❌ | ✅ | ❌ |
| Backup password | ✅ | ❌ | ❌ | only while typing | ❌ |
| DEK | – | ✅ while in use | ❌ | ✅ while processing | ❌ |
| Plain data | ✅ | ✅ | ❌ | ❌ (never sent) | ❌ |
| Associated Data (e.g. file title) | ✅ | ✅ | ❌ | ✅ (shown) | ❌ |
| Session Key (AES) | – | ✅ | ❌ | ✅ | ❌ (only in QR code) |
| MA Device private key | – | ❌ | ❌ | ✅ | ❌ |
| CA ephemeral private key | – | ✅ | ❌ | ❌ | ❌ |
| Tunnel Id, App Name, IP, timing, message sizes | – | ✅ | ✅ | ✅ | ❌ |

---

Next: [Architecture →](04-architecture.md)
