# Qrs.Tunnel.Server

QRS Tunnel Server � the zero-knowledge encrypted tunnel used by QRS Client
Applications and the QRS Mobile App to exchange encrypted Requests/Responses.
See the [repository root README](../README.md) and
[`.github/copilot-instructions.md`](../.github/copilot-instructions.md) for
the full architecture overview.

## Deploy to Google Cloud Run

[![Run on Google Cloud](https://deploy.cloud.run/button.svg)](https://deploy.cloud.run/?git_repo=https://github.com/bitexperts/QRID&revision=development&dir=Qrs.Tunnel.Server)

Clicking the button above takes you to the Google Cloud Run "Deploy from
source" flow, which clones this repository, builds the image using
[`cloudbuild.yaml`](./cloudbuild.yaml) (which in turn builds
[`Dockerfile.gcr`](./Dockerfile.gcr) from the repo root), and deploys it as a
Cloud Run service.

### How it works

- `cloudbuild.yaml` builds `Dockerfile.gcr` with the repo root as the Docker
  build context, because Qrs.Tunnel.Server's project references span
  multiple sibling folders and the full source tree is needed for restore.
- `Dockerfile.gcr` publishes the server with .NET Native AOT
  (`PublishAot=true`) for the fastest possible cold start on serverless
  platforms like Cloud Run.
- The resulting image is fully compliant with the
  [Cloud Run Container Contract](https://docs.cloud.google.com/run/docs/container-contract):
  it listens on the port supplied via the `$PORT` environment variable at
  container runtime instead of a hard-coded port.

### Prerequisites

- The `bitexperts/QRID` repository (or at least this directory) must be
  publicly readable, since Cloud Build clones it anonymously during the
  deploy-from-source flow.
- A GCP project with billing enabled. The deploy flow will prompt to enable
  the Cloud Run, Cloud Build, and Artifact Registry APIs if they aren't
  already enabled.

### Customizing the deploy

The button link accepts standard Cloud Run deploy-from-source query
parameters, for example:

```
https://deploy.cloud.run/?git_repo=https://github.com/bitexperts/QRID&revision=development&dir=Qrs.Tunnel.Server&min-instances=0
```

- `revision` � branch or tag to deploy (defaults to `development` above;
  switch to a release tag for production deployments).
- `dir` � must remain `Qrs.Tunnel.Server` so Cloud Build picks up
  `cloudbuild.yaml` from this folder.

`cloudbuild.yaml` also exposes substitutions you can override in the Cloud
Build trigger/UI:

| Substitution | Default | Purpose |
|---|---|---|
| `_SERVICE_NAME` | `qrs-tunnel-server` | Cloud Run service / image name |
| `_REGION` | `us-central1` | Cloud Run region |
| `_RUNTIME_IDENTIFIER` | `linux-x64` | .NET RID for the Native AOT publish (use `linux-arm64` for arm64 Cloud Run) |

### Building and running locally

```bash
docker build -f Qrs.Tunnel.Server/Dockerfile.gcr -t qrs-tunnel-server .
docker run -e PORT=4999 -p 4999:4999 qrs-tunnel-server
```

### Building locally then publishing to Docker Hub 

Note: only QRID team could push to qridcom repository at docker hub.
Could be done from bash or powershell:

```bash
docker build -f Qrs.Tunnel.Server/Dockerfile.gcr -t qrs-tunnel-server .
docker tag %IMAGE_ID% qridcom/qrs-tunnel-server:latest
docker push qridcom/qrs-tunnel-server:latest
```

## Configuration

Settings live under the `QrsTunnel` section of `appsettings.json` and can be
overridden with environment variables (using the `QrsTunnel__<Key>` convention).

| Key | Env var | Default | Purpose |
|---|---|---|---|
| `QrsTunnel:MaxSessionCount` | `QrsTunnel__MaxSessionCount` | `10000` | Maximum number of concurrent tunnel sessions. |
| `QrsTunnel:ApiKey` | `QrsTunnel__ApiKey` | *(empty)* | Optional API key protecting the session-creation flow. |

### Optional API key

When `QrsTunnel:ApiKey` is empty the server behaves exactly as before and any
client may open a session. When it is set, a request that would **create a new**
tunnel session must present a matching value in the `qrs_tunnel_api_key` cookie;
otherwise the request is rejected. This protects the server from a
memory-overflow (too-many-sessions) attack.

Requests that only read from or write to an **already existing** session are
never checked. This means only Client Applications (which open sessions) need to
be configured with the key — the Mobile App, which only reads/writes sessions
created by a Client Application, does not need it. On the client side, the key is
provided as a setting alongside the tunnel service URI and is sent automatically
as the `qrs_tunnel_api_key` cookie.

