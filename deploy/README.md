# Self-hosted deployment

An alternative to Azure Container Apps: build images in CI, push them to a registry, and run
them behind a shared Traefik on any Docker host.

```
deploy/
  Dockerfile.web           multi-stage build of src/Web (compiles the SPA when present)
  Dockerfile.migration     runs EF migrations + the seeder, then exits
  docker-compose.yml       postgres + migration + web, published through Traefik
  deploy.sh                idempotent pull -> migrate -> restart -> health check
  .env.example             every configuration value the stack needs
  traefik/                 shared reverse proxy, deployed once per host
```

## First-time host setup

```bash
docker network create cleanarchitecture_web

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

## CI

`.github/workflows/_deploy.yml` is a reusable workflow: it builds both images, pushes them to
GHCR tagged with the commit SHA, then runs `deploy.sh` over SSH on a host that has this
directory checked out. Call it from an environment-specific workflow.
