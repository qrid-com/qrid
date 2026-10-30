# Roadmap

This roadmap is a statement of intent, not a commitment. Items move as we learn from users and
contributors. Discussion and help are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md).

## Phase 0 — Open-sourcing readiness

- [ ] Add a `LICENSE` file (MIT) and SPDX headers.
- [ ] Publish rendered docs from `qrs-doc/`.
- [ ] Split or clearly scope QRS projects (`QRSHL.sln`) from QRID‑only projects.
- [ ] Update CI (`qrs-intest.yml`) to install .NET 10 and build `QRSHL.sln`.
- [ ] Remove development‑only settings (sideloading markers, cleartext HTTP exceptions) from release builds.
- [ ] Issue and pull‑request templates, `CODEOWNERS`.

## Phase 1 — Security hardening 

- [ ] Independent third‑party security audit.

## Phase 2 — Protocol evolution

- [ ] Official **test vectors** for all primitives, envelope and tunnel messages.
- [ ] Formal protocol specification (RFC‑style) derived from these docs.

## Phase 3 — Client Applications

- [ ] Develop new features for `VAULT`, `PASSWORD MANAGER`, `LAUNCHER`, `ASKPASS` applications.

## Phase 4 — Client SDK

- [ ] Java Client SDK.
- [ ] TypeScript/WebCrypto Client SDK (browser and Node.js).
- [ ] Python and Go Client SDKs.
