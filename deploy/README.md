# Self-hosted deployment

An alternative to Azure Container Apps: build images in CI, push them to a registry, and run
them behind a shared Traefik on any Docker host.

```
deploy/
  Dockerfile.web           multi-stage build of src/Web (compiles the SPA when present)
  Dockerfile.migration     runs EF migrations + the seeder, then exits
  docker-compose.yml       postgres + migration + web, published through Traefik
  deploy.sh                idempotent pull -> migrate -> restart -> health check
  docker-compose.cloudflared.yml  cloudflared instead of Traefik, for a pre-production host
  Deploy-Preprod.ps1       build on the PC, ship over SSH, deploy (no registry)
  setup-host.sh            prepare a pre-production Docker host, once
  .env.example             every configuration value the stack needs
  traefik/                 shared reverse proxy, deployed once per host
```

## First-time host setup

```bash
docker network create ca-shared-web

cd deploy/traefik
cp .env.example .env && ${EDITOR:-vi} .env    # set ACME_EMAIL
docker compose --env-file .env up -d
```

## Per-environment setup

```bash
cd deploy
cp .env.example .env && ${EDITOR:-vi} .env    # set STACK_NAME, PUBLIC_HOST, POSTGRES_PASSWORD, ...
./deploy.sh
```

`PUBLIC_HOST` must already resolve to this host before the first run: Traefik obtains the
certificate through an ACME TLS challenge on port 443.

## Configuration

`.env` is the single place where the stack's configuration lives. Keys map onto ASP.NET
configuration paths, `__` separating sections — `Cors__AllowedOrigins__0`, for example. The
compose file assembles `ConnectionStrings__CleanArchitectureDb` from the `POSTGRES_*` values so
the connection string is never written down twice.

Two settings matter beyond the obvious ones:

- `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` — Traefik terminates TLS and forwards plain HTTP.
  Without it the app sees `http`, issues insecure redirects, and marks cookies non-secure.
- The Data Protection key ring is persisted to the database (`DataProtectionKeys` table), so
  auth cookies survive redeploys and can be shared across replicas.

## Pre-production on a machine of your own (Ubuntu + Cloudflare Tunnel)

No hosting bill and no code change: a Docker host on the local network runs the same stack, a
Cloudflare Tunnel exposes it over HTTPS under your public hostname without opening a port on the
router or depending on the public IP, and `Deploy-Preprod.ps1` builds the images on the
development PC (Docker Desktop, 3-4 GB of RAM for the .NET + npm build) and ships them over SSH.
The host only needs Docker.

### The host, once

`setup-host.sh` installs Docker if missing, creates the `deploy` user (no password, no sudo,
member of the `docker` group, SSH key login only), creates `/opt/cleanarchitecture-preprod` and
drops a `.env` there to fill in. From the PC, with an administrator account on the host:

```powershell
scp deploy/setup-host.sh deploy/.env.example <admin>@<host>:/tmp/
ssh -t <admin>@<host> sudo bash /tmp/setup-host.sh "'$(Get-Content ~/.ssh/id_ed25519.pub)'"
```

`scp`, `ssh` and then `sudo` each prompt for the admin password (`-t` allocates the terminal
`sudo` needs). `DEPLOY_USER=...` and `ENV_DIR=...` in front of the script change the account and
the directory; the PowerShell script's `-RemoteDir` must then follow. Then, on the host, fill in
`/opt/cleanarchitecture-preprod/.env`: `STACK_NAME`, `DOTNET_ENVIRONMENT`, `PUBLIC_HOST`,
`CLOUDFLARE_TUNNEL_TOKEN` (below), `POSTGRES_PASSWORD` and `SMTP_*`. `REGISTRY`,
`IMAGE_NAMESPACE` and `IMAGE_TAG` have no effect here: the script forces the names of the
images it has just loaded.

The `docker` group is equivalent to root on the host: the `deploy` account only deploys, from a
PC whose private key is protected.

### The Cloudflare Tunnel, once

1. The domain must be served by Cloudflare DNS: add it to the Cloudflare dashboard (free plan
   is enough) and point your registrar's name servers at the two Cloudflare gives you.
2. Zero Trust > Networks > Tunnels > *Create a tunnel* (Cloudflared). Copy the token from the
   "Install connector" step into `CLOUDFLARE_TUNNEL_TOKEN` in the `.env`.
3. *Public Hostname* tab: your hostname, type **HTTP**, URL **`http://web:8080`** (the compose
   service name, not `localhost`: cloudflared runs in its own container). Cloudflare creates the
   CNAME record itself.

`docker-compose.cloudflared.yml` adds a `cloudflared` service that holds the outbound connection
to Cloudflare and forwards to `web` on the internal network: nothing is published on the host.
Cloudflare terminates TLS and sends `X-Forwarded-For/Proto`, which the application already reads
(`ASPNETCORE_FORWARDEDHEADERS_ENABLED`).

### Every release, from the PC

```powershell
.\deploy\Deploy-Preprod.ps1 -Target deploy@<host-or-ip>
```

Builds `local/cleanarchitecture/{web,migration}:<sha>`, saves them to a tar, copies it with
`scp` and loads it with `docker load` on the host, copies `docker-compose.yml`,
`docker-compose.cloudflared.yml` and `deploy.sh` into `/opt/cleanarchitecture-preprod`, then runs
`IMAGE_TAG=<sha> SKIP_PULL=1 COMPOSE_EXTRA_FILES=docker-compose.cloudflared.yml COMPOSE_EXTRA_SERVICES=cloudflared ./deploy.sh`.
The host's `.env` is never touched. A tag already on the host is not sent again; a tree with
uncommitted changes is tagged `<sha>-dirty` and always sent. `-SkipBuild` reuses the local
images; `-RemoteDir` and `-EnvFile` pick another directory or environment file.

Old images pile up on the host: run `docker image prune -f` now and then.

With `DOTNET_ENVIRONMENT=Development` the seeder creates test accounts with known passwords on a
public URL: only invite testers you trust, or change those passwords after the first seed.

## CI

`.github/workflows/_deploy.yml` is a reusable workflow: it builds both images, pushes them to
GHCR tagged with the commit SHA, then runs `deploy.sh` over SSH on a host that has this
directory checked out. Call it from an environment-specific workflow.
