# 8. Pairing & Key Switching

Pairing is how a Client Application (CA) learns the **public Device Key** of the user's Mobile App
(MA), so that all later payloads can be encrypted to that key in addition to the Session Key from
the QR code. Pairing happens **inside the same tunnel session** as the first command; the user
does not have to scan a second QR code.

- [Why pairing exists](#why-pairing-exists)
- [Keys involved](#keys-involved)
- [User experience](#user-experience)
- [Flow A: CA has no paired key](#flow-a-ca-has-no-paired-key)
- [Flow B: key switching (CA knows several phones)](#flow-b-key-switching-ca-knows-several-phones)
- [Flow C: switch rejected, fall back to pairing](#flow-c-switch-rejected-fall-back-to-pairing)
- [State machines](#state-machines)
- [Rules and limits](#rules-and-limits)
- [Storage of paired keys](#storage-of-paired-keys)
- [Resetting pairing](#resetting-pairing)
- [Security analysis](#security-analysis)

---

## Why pairing exists

The QR code transfers the Session Key over an optical channel, which is excellent against network
attackers but can be observed by a camera or a person looking over your shoulder. After pairing,
every payload is **also** encrypted with hybrid ML‑KEM‑768 + ECDH P‑256 to the phone's Device
Key, whose private part never leaves the phone. An observer of the QR code therefore still cannot
read the requests.

```mermaid
flowchart LR
    subgraph Before["Unpaired first message"]
        A1["Pairing-Requested-CA"] --> B1["Session Key only"]
    end
    subgraph After["Every payload after pairing"]
        A2["KeyOpRequest"] --> B2["Hybrid PQC to Device Key"] --> C2["Session Key"] --> D2["TLS"]
    end
```

## Keys involved

| Key | Owner | Role in pairing |
|---|---|---|
| Device Key pair (hybrid) | MA | Its public part is what is being paired. |
| Tunnel ephemeral key pair (hybrid) | CA | MA encrypts the `PairingRequest` to it (verified via `cph` in QR). |
| Session Key | CA (in QR) | Outer layer for all messages. |
| Pairing code | MA | 3 random lower‑case letters shown on the phone, typed into the CA. |

## User experience

```mermaid
journey
    title First use of a new Client Application
    section Client Application
      User starts an action (e.g. Open vault): 5: User
      QR code appears: 5: User
    section Mobile App
      Scan QR code: 5: User
      Pairing explanation screen, tap Continue Pairing: 4: User
      Phone shows a 3-letter code: 4: User
    section Client Application
      App asks for pairing code, user types it: 3: User
    section Mobile App
      Tap Continue: 4: User
      Review request details, tap Yes: 5: User
    section Client Application
      Action completes: 5: User
```

## Flow A: CA has no paired key

```mermaid
sequenceDiagram
    autonumber
    actor U as User
    participant CA as Client App
    participant TS as Tunnel Server
    participant MA as Mobile App

    CA->>U: show QR
    CA->>TS: CA2MA #1 Pairing-Requested-CA (Session Key only), poll 120 s
    U->>MA: scan QR
    MA->>TS: MA2CA #1 poll 2 s
    TS-->>MA: CA2MA #1 + CA ephemeral public key
    MA->>MA: verify SHA3(CA pk) == cph, decrypt: IsPairingRequestedByCa
    MA->>MA: code = 3 random letters<br/>PairingRequest{Device pk, KeyHashId, code}
    MA->>TS: MA2CA #2 Pairing-Requested-MA (hybrid to CA pk + Session Key), no wait
    TS-->>CA: MA2CA #2
    CA->>CA: decrypt both layers, IsPairingRequestedByMa
    MA->>U: Pairing screen, then code "abc"
    CA->>U: prompt "pairing-code"
    U->>CA: types "abc"
    CA->>CA: code matches: save Device pk, set most recent key
    CA->>TS: CA2MA #2 Payload(KeyOpRequest) hybrid to Device pk + Session Key, poll 120 s
    U->>MA: tap Continue (DoNotResetMessageNumbers = true)
    MA->>TS: MA2CA #3 poll 30 s
    TS-->>MA: CA2MA #2
    MA->>MA: decrypt with Device private key: IsPayload
    MA->>U: show request, user taps Yes
    MA->>TS: MA2CA #4 Payload(KeyOpResponse)
    TS-->>CA: MA2CA #4
```

## Flow B: key switching (CA knows several phones)

A CA can have several paired keys (e.g. the user reinstalled the MA, or uses two phones with the
same Master Key restored from backup). The CA always tries the **most recently used** key first.
If the scanning phone has a different Device Key, the MA cannot decrypt layer A and asks the CA to
switch to its key **by id only** (no public key is sent, so no user code is needed):

```mermaid
sequenceDiagram
    autonumber
    participant CA as Client App
    participant TS as Tunnel Server
    participant MA as Mobile App (phone 2)

    CA->>TS: CA2MA #1 Payload encrypted to phone 1 key
    MA->>TS: MA2CA #1 poll
    TS-->>MA: CA2MA #1
    MA->>MA: layer A key_id != own KeyHashId (IsKeyIdDifferent)
    MA->>TS: MA2CA #2 SwitchPubKey-Requested(own KeyHashId), poll 60 s
    TS-->>CA: MA2CA #2
    CA->>CA: CanSwitchPubKey and key known: ReceiverAsym = phone 2 key,<br/>set most recent key id
    CA->>TS: CA2MA #2 Payload encrypted to phone 2 key, poll 120 s
    TS-->>MA: CA2MA #2
    MA->>MA: IsPayload, user approves
    MA->>TS: MA2CA #3 Payload(KeyOpResponse)
    TS-->>CA: MA2CA #3
```

## Flow C: switch rejected, fall back to pairing

```mermaid
sequenceDiagram
    autonumber
    participant CA as Client App
    participant TS as Tunnel Server
    participant MA as Mobile App (new phone)

    CA->>TS: CA2MA #1 Payload encrypted to old key
    MA->>TS: poll
    TS-->>MA: CA2MA #1 (IsKeyIdDifferent)
    MA->>TS: MA2CA SwitchPubKey-Requested(KeyHashId), poll 60 s
    TS-->>CA: SwitchPubKey-Requested
    CA->>CA: key id unknown: clear ReceiverAsym
    CA->>TS: CA2MA #2 SwitchPubKey-Rejected (Session Key only), poll 120 s
    TS-->>MA: SwitchPubKey-Rejected
    MA->>MA: StartPairing()
    MA->>TS: MA2CA Pairing-Requested-MA (code, Device pk)
    Note over CA,MA: continues exactly like Flow A from step 8
```

## State machines

### Client Application (`ClientContext.MainFlowAsync`)

```mermaid
stateDiagram-v2
    [*] --> ShowQR
    ShowQR --> NeedPairing: no paired key
    ShowQR --> SendPayload: paired key loaded
    NeedPairing --> AwaitPairingRequest: send Pairing-Requested-CA
    AwaitPairingRequest --> AskCode: Pairing-Requested-MA
    AwaitPairingRequest --> Failed: other / timeout
    AskCode --> SavePairedKey: code matches
    AskCode --> Failed: code mismatch / cancelled
    SavePairedKey --> SendPayload
    SendPayload --> Done: Payload response
    SendPayload --> Switch: SwitchPubKey-Requested
    SendPayload --> Failed: error / timeout
    Switch --> SendPayload: key id known (once per call)
    Switch --> Rejected: key id unknown
    Rejected --> AskCode: Pairing-Requested-MA (once per call)
    Rejected --> Failed: other
    Done --> [*]
    Failed --> [*]
```

### Mobile App (`QrsAppContext.PayloadOrPairing`)

```mermaid
stateDiagram-v2
    [*] --> Validate: QR scanned
    Validate --> Error: bad URL / unsupported version
    Validate --> Poll1: alerts collected, poll 2 s
    Poll1 --> Review: IsPayload
    Poll1 --> SwitchReq: IsKeyIdDifferent
    Poll1 --> Pairing: IsPairingRequestedByCa
    Poll1 --> Error: other / timeout
    SwitchReq --> Review: IsPayload
    SwitchReq --> Pairing: IsSwitchRejected
    SwitchReq --> Error: other
    Pairing --> ShowCode: Pairing-Requested-MA sent
    ShowCode --> Poll2: user taps Continue (no counter reset)
    ShowCode --> [*]: Cancel
    Poll2 --> Review: IsPayload
    Poll2 --> Error: other
    Review --> SendResponse: user taps Yes
    Review --> [*]: user taps No (CA times out)
    SendResponse --> [*]
    Error --> [*]
```

## Rules and limits

| Rule | Where | Reason |
|---|---|---|
| Switch or pair **at most once** per command | `switchAllowed`, `pairingAllowed` in `MainFlowAsync` | Prevent loops driven by a malicious peer. |
| Switch/pair only when `CommandCount == 0` (`CanSwitchPubKey`, `CanPair`) | `ClientContext` | After the first successful command in an app session the phone is fixed; a different phone requires restarting the CA. Prevents a silent swap to an attacker's phone mid‑session. |
| Pairing payload must come from **layer A** | `MsgHelper.IsPairingRequestedByMa` | Only a party that received the CA's ephemeral key via the tunnel **and** knows the Session Key can produce it. |
| `SHA3(SenderPubKeyBytes) == cph` | `EnsureReceiverPublicKeyForQrV2` | The MA encrypts to the key the QR code vouches for, not to a key injected by the server. |
| MA refuses non‑PQC Device Keys | `MobUtils.LoadDeviceKeyItem` | Guarantees hybrid PQC for layer A. |

## Storage of paired keys

The CA persists paired keys through `IAsymKeyItemProvider`. The default
`FileAsymKeyItemProvider` stores, in the app's data folder:

| File | Content |
|---|---|
| `_asym-key-item-<KeyHashId>` | The paired public key (`AsymKeyItem`) |
| `_most-recent-key-id` | `KeyHashId` of the key to try first |

Paired keys are **public**; what matters is their **integrity**. Protect the folder from tampering
by other users on the same machine (standard user‑profile permissions).

## Resetting pairing

On the MA, **Actions → Reset Pairing** generates a new Device Key. All CAs will then receive a
`SwitchPubKey-Requested` with an unknown id and fall back to pairing on next use. Do this if you
suspect a CA machine was compromised or you want to revoke all CAs at once.

## Security analysis

| Threat | Mitigation | Residual risk |
|---|---|---|
| Network attacker / malicious Tunnel Server injects its own "Device Key" | Needs the Session Key (QR only) to produce a valid layer‑B message, and needs to make the user type a matching code | Low |
| Camera observes QR **and** controls the Tunnel Server during first pairing | Pairing code is shown only on the phone and must be typed by the user into the CA | Guessing: 26³ = 17,576 codes, one attempt per QR scan (the CA aborts on mismatch) |
| Attacker swaps phones after the first command | `CanPair`/`CanSwitchPubKey` false when `CommandCount > 0` | None within an app session |
| Stale / revoked phone | Reset Pairing on the MA; delete key files on the CA | — |

---

Next: [Mobile App Internals →](09-mobile-app.md)
