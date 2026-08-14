#!/usr/bin/env bash
# Idempotent deployment: pull the tagged images, run migrations, restart the web app.
# Safe to re-run; re-running with the same IMAGE_TAG is a no-op beyond a restart.
#
#   ./deploy.sh                 # uses ./.env
#   ./deploy.sh .env.prod       # uses another env file
set -euo pipefail

cd "$(dirname "$0")"

ENV_FILE="${1:-.env}"
if [[ ! -f "$ENV_FILE" ]]; then
  echo "Env file '$ENV_FILE' not found. Copy .env.example and fill it in." >&2
  exit 1
fi

COMPOSE=(docker compose --env-file "$ENV_FILE" -f docker-compose.yml)

# Compose interpolation gives shell variables precedence over the env file, so CI can override
# IMAGE_TAG with the commit SHA without rewriting .env.
if [[ -n "${GHCR_TOKEN:-}" ]]; then
  echo "$GHCR_TOKEN" | docker login ghcr.io -u "${GHCR_USER:?GHCR_USER required with GHCR_TOKEN}" --password-stdin
fi

# The shared Traefik network is created once per host, outside any stack.
docker network inspect cleanarchitecture_web >/dev/null 2>&1 \
  || docker network create cleanarchitecture_web

echo "==> Pulling images"
"${COMPOSE[@]}" pull

echo "==> Starting database"
"${COMPOSE[@]}" up -d postgres

echo "==> Applying migrations"
# --exit-code-from propagates the migration container's exit code, so a failed migration
# fails the deployment instead of silently leaving the old app running against a new schema.
"${COMPOSE[@]}" up --no-deps --abort-on-container-exit --exit-code-from migration migration

echo "==> Starting web"
"${COMPOSE[@]}" up -d web

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
