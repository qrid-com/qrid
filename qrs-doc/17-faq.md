# 17. Frequently Asked Questions

- [General](#general)
- [Security](#security)
- [Using QRS](#using-qrs)
- [Developers](#developers)
- [Operations](#operations)
- [Project](#project)

---

## General

### What is QRS in one sentence?

A way to encrypt data in any application with a key that lives only on your phone: to open the
data you scan a QR code and tap **YES** — no passwords, no accounts, post‑quantum encryption.

### How does QRS relate to QRID?

QRS is the **envelope‑encryption** sibling of the QRID identity platform. QRS needs no domains, 
accounts or identity servers — it only needs a Tunnel Server.

### How is QRS different from a password manager?

A password manager stores secrets behind a master password. QRS **removes the password**: the
Master Key is a random 256‑bit key held by your phone, and every use is approved on the phone.
Your data can live anywhere (local disk, cloud drive, e‑mail) because it is always stored
encrypted. Of cource, we also have a PASSWORD MANAGER as one of the applications in this repo.

### How is it different from passkeys / FIDO2?

Passkeys **authenticate** you to a server. QRS **encrypts data** so that nobody — including any
server — can read it without your phone. The two are complementary.

### Do I need an account or an internet connection?

No account. The Client Application and the phone need to reach the same **Tunnel Server**
(internet or local network). The data itself never goes through the tunnel — only the wrapped
DEK and the unwrapped DEK, both encrypted with 2 layers of post-quantum E2EE, plus 3rd layer 
of TLS provided by your OS. Sounds very secure, we know.

## Security

### Is QRS really post-quantum?

Everything that crosses the network is protected by **ML‑KEM‑768 (FIPS 203) + ECDH P‑256** hybrid
encryption and AES‑256‑GCM. Please see [Cryptography](05-cryptography.md#postquantum-assessment).

### Can the Tunnel Server operator read my data?

No. The server only relays ciphertext encrypted with keys it never sees (session key in the QR
code + hybrid public‑key encryption). It can see timing, message sizes, IP addresses, the name 
of the QRS app being used, and application Instance ID (for rate limiting and basic metrics 
running server-side).

### What if someone photographs the QR code?

They get the one of the session keys (symmetric) for **that one session only**. The DEK returned by the phone is
additionally encrypted to the Client Application's ephemeral private key (asymmetric), which is never in the
QR code. So your data are still safe. Please see [Security Model](15-security-model.md).

### What if I lose my phone?

Restore your Master Key on a new phone from the **encrypted backup** you created (backup image +
backup password). Without a backup, the data is **unrecoverable** — by design, there is no
server copy and no back door.

### What if my phone is stolen?

The thief needs to unlock the phone. The Master Key is in the OS secure storage (Keychain /
Android Keystore). Restore from backup on a new phone. If you suspect the Master Key was
extracted, decrypt sensitive data and re‑encrypt it under a new Master Key. Leaked Master Key by itself 
does not leak any of your data, because attacker would also need to get hold of your encrypted data.

### What are the known weaknesses?

Please see [Security Model](15-security-model.md#known-limitations).

### Has QRS been audited?

Not yet. An independent audit is on the [Roadmap](../ROADMAP.md). Reviews by the community are very
welcome — see [SECURITY.md](../SECURITY.md).

## Using QRS

### Which apps support QRS today?

The reference apps: Vault, `qrs` process launcher, ssh/sudo askpass helpers, Password Manager — 
please see [Reference Applications](12-reference-apps.md).

### Can I use one phone for many apps and computers?

Yes. One Master Key protects DEKs for all apps. Each app is paired with the phone once.

### Can I have several Master Keys?

The Mobile App works with one loaded Master Key at a time; each envelope records the `kek_name`
of the key that wrapped it, and a mismatch is reported clearly. You could have multiple Master Keys 
and use "Actions > Logout" then "Load Existing Master Key" to load another key.

### Can two people share an encrypted file?

Not directly in the current version: an envelope is bound to one Master Key. 
For multi‑user E2EE with identities, see the QRID platform.

### Why do I sometimes not need to scan when saving?

Apps like Vault keep the DEK in memory while a document is open and re‑encrypt locally on save
(`GenerateNewDekOnModifiedPayload = false`), as long as the displayed metadata has not changed.

## Developers

### What languages are supported?

The reference SDK is **.NET 10** (C#). The protocol is language‑agnostic (Protobuf + standard
primitives); see the [Porting Guide](16-porting-guide.md).

### How much code do I need to add QRS to my app?

Typically: a `ClientContext` subclass, a `Wrapper<T>` for your payload, and an
`IUserInteraction` implementation (Avalonia UI and WinForms implementations are provided) that shows a QR code 
and asks for a pairing code. See the [Client SDK](10-client-sdk.md) quick start.

### Can I run everything locally?

Yes — run the Tunnel Server on your machine and point the app and phone at it. For automated
tests, `Qrs.TestConsole` simulates the phone ([Building and testing](14-building-and-testing.md)).

### Where do I report bugs?

Functional bugs: GitHub issues. Security problems: privately, per [SECURITY.md](../SECURITY.md).

## Operations

### Do I have to run my own Tunnel Server?

No, by default it would use one of the public servers provided by QRID. Running your own is easy (one small container, no
database) — see [Operating the Tunnel Server](13-tunnel-server-ops.md).

### How much does it cost to run a Tunnel Server?

It fits comfortably in free tiers (e.g. Google Cloud Run with Native AOT, cold start in 200 ms, configured to scale to zero). 
Traffic is tiny: a few kilobytes per operation.

### Can it scale horizontally?

Sessions are in memory; for now, use a single instance. See
[Scaling](13-tunnel-server-ops.md#scaling).

## Project

### What is the license?

QRS is intended to be released under the **MIT License**, as announced on the QRID website.
**The repository does not contain a `LICENSE` file yet**; adding it is part of the open‑sourcing
checklist in the [Roadmap](../ROADMAP.md).

### How can I contribute?

Read [CONTRIBUTING.md](../CONTRIBUTING.md) and our [Code of Conduct](../CODE_OF_CONDUCT.md). Good first
contributions: documentation fixes, ports to other languages, test vectors.

### Who is behind QRS?

The QRID team - we are based in New York City, United States, and Toronto, Canada. Contact: info@qrid.com.

### Where can I see a demo?

<https://www.youtube.com/watch?v=PZVwYSCO1p4>

---

Back to the [documentation index](../README.md).
