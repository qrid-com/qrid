# Security Policy

QRS is security software. We take every report seriously and appreciate responsible disclosure.

## Reporting a vulnerability

**Please do not open public GitHub issues for security problems.**

Report privately through one of:

1. GitHub **Private vulnerability reporting** ("Security" tab → "Report a vulnerability") once it
   is enabled for the public repository, or
2. E‑mail **info@qrid.com** with the subject `SECURITY: <short summary>`.

Please include:

- affected component (Mobile App, Client SDK, Tunnel Server, a reference app, protocol/spec),
- version / commit,
- description, impact and, if possible, a proof of concept,
- whether the issue is already public.

We will credit reporters in the release notes unless you prefer to stay anonymous.

## Scope

In scope:

- Cryptographic design and implementation (`Qrid.Common/Crypto`, envelope, tunnel encryption).
- Mobile App key handling, backup and consent UI.
- Client SDK (`Qrs.Client.Context`, `Qrs.Tunnel.Client`, `Qrs.Discovery.Client`).
- Tunnel Server (`Qrs.Tunnel.Server`).
- Reference applications.

Already documented weaknesses are listed in
[Security Model → Known limitations](qrs-doc/15-security-model.md#known-limitations); reports that add new
attack paths or impact to them are still welcome.

Out of scope: attacks requiring a compromised phone OS,  denial of service by volumetric flooding.

## Supported versions

Until the first stable release, only the latest `main` branch receives security fixes.

## Safe harbour

We will not pursue legal action against good‑faith research that respects user privacy, avoids
data destruction and service disruption, and gives us reasonable time to fix issues before
disclosure.
