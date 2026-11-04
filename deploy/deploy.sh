#!/usr/bin/env bash
# Idempotent deployment: pull the tagged images, run migrations, restart the web app.
# Safe to re-run; re-running with the same IMAGE_TAG is a no-op beyond a restart.
#
#   ./deploy.sh                 # uses ./.env
#   ./deploy.sh .env.prod       # uses another env file
#   IMAGE_TAG=abc1234 SKIP_PULL=1 COMPOSE_EXTRA_FILES=docker-compose.cloudflared.yml COMPOSE_EXTRA_SERVICES=cloudflared ./deploy.sh
#                               # images loaded locally, exposed through a Cloudflare Tunnel (Deploy-Preprod.ps1)
set -euo pipefail

cd "$(dirname "$0")"

ENV_FILE="${1:-.env}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Env file '$ENV_FILE' not found. Copy .env.example and fill it in." >&2
  exit 1
fi

COMPOSE=(docker compose --env-file "$ENV_FILE" -f docker-compose.yml)

# Extra compose files layered on top, space-separated: COMPOSE_EXTRA_FILES=docker-compose.cloudflared.yml
# adds the tunnel instead of Traefik. Not in .env: compose does not read -f from it.
for extra in ${COMPOSE_EXTRA_FILES:-}; do
  COMPOSE+=(-f "$extra")
done

# Compose interpolation gives shell variables precedence over the env file, so CI can override
# IMAGE_TAG with the commit SHA without rewriting .env.
if [[ -n "${GHCR_TOKEN:-}" ]]; then
  echo "$GHCR_TOKEN" | docker login ghcr.io -u "${GHCR_USER:?GHCR_USER required with GHCR_TOKEN}" --password-stdin
fi

# The shared Traefik network is created once per host, outside any stack.
docker network inspect ca-shared-web >/dev/null 2>&1 \
  || docker network create ca-shared-web

# SKIP_PULL=1 when the images were loaded by hand (docker load): they exist in no registry.
if [[ "${SKIP_PULL:-0}" != "1" ]]; then
  echo "==> Pulling images"
  "${COMPOSE[@]}" pull
fi

# COMPOSE_EXTRA_SERVICES names what an override adds next to web (cloudflared). Their images come
# from a registry even when the app images were loaded by hand, and a home connection drops a
# pull now and then: retry before giving up, rather than after the migration has run.
if [[ -n "${COMPOSE_EXTRA_SERVICES:-}" ]]; then
  echo "==> Pulling ${COMPOSE_EXTRA_SERVICES}"
  for attempt in 1 2 3; do
    # shellcheck disable=SC2086
    if "${COMPOSE[@]}" pull ${COMPOSE_EXTRA_SERVICES}; then break; fi
    if [[ $attempt -eq 3 ]]; then echo "Could not pull ${COMPOSE_EXTRA_SERVICES}." >&2; exit 1; fi
    echo "Pull failed, retrying in 10 s (attempt $attempt/3)" >&2
    sleep 10
  done
fi

echo "==> Starting database"
"${COMPOSE[@]}" up -d postgres

echo "==> Applying migrations"
# --exit-code-from propagates the migration container's exit code, so a failed migration
# fails the deployment instead of silently leaving the old app running against a new schema.
"${COMPOSE[@]}" up --no-deps --abort-on-container-exit --exit-code-from migration migration

echo "==> Starting web"
# Started here with web, not with a bare `up -d`, which would also restart the completed
# migration container.
# shellcheck disable=SC2086
"${COMPOSE[@]}" up -d web ${COMPOSE_EXTRA_SERVICES:-}

echo "==> Waiting for health"
for _ in $(seq 1 30); do
  if [[ "$("${COMPOSE[@]}" ps -q web | xargs docker inspect -f '{{.State.Health.Status}}')" == "healthy" ]]; then
    echo "Deployment healthy."
    exit 0
  fi
  sleep 5
done

echo "Web container did not become healthy in time." >&2
"${COMPOSE[@]}" logs --tail 100 web >&2
exit 1
