# Changelog

All notable changes to QRS will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). The tunnel **message
protocol version** is versioned separately (see
[Protocol versioning](qrs-doc/06-tunnel-protocol.md#protocol-versioning)).

## [Unreleased]

### Added

- Developer documentation set in `qrs-doc/` (concepts, architecture, cryptography, tunnel
  protocol, key operations, pairing, Mobile App, Client SDK, class diagrams, reference apps,
  operations, building/testing, security model, porting guide, FAQ, community files).

### Current state (baseline before first public release)

- Tunnel message protocol version `1.0.0`; QR command format `v=2`.
- Hybrid ML‑KEM‑768 + ECDH P‑256 tunnel encryption; AES‑256‑GCM everywhere.
- Tunnel AEAD `"{Direction}:{MessageNumber}:{ProtocolVersion}"` with peer version pinned per session.
- `QrsEnvelope` with `schema_version` unset (0).
- .NET 10 for all QRS projects; Native AOT Tunnel Server image for Cloud Run.
