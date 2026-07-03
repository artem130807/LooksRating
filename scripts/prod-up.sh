#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILE="docker-compose.prod.yml"
API_REPLICAS="${API_REPLICAS:-2}"
export COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-looksrating}"

if [[ ! -f .env ]]; then
  echo "Missing .env — copy from .env.example and set secrets." >&2
  exit 1
fi

if [[ ! -f TGgifts-buyer/.env ]]; then
  echo "Missing TGgifts-buyer/.env — copy from TGgifts-buyer/example.env" >&2
  exit 1
fi

echo "==> Infrastructure (postgres, redis, kafka)..."
docker compose -f "$COMPOSE_FILE" up -d \
  postgres redis zookeeper kafka ticket-postgres

docker compose -f "$COMPOSE_FILE" up -d --wait \
  postgres redis zookeeper kafka ticket-postgres

echo "==> API: 1 replica (migrations)..."
docker compose -f "$COMPOSE_FILE" build api
docker compose -f "$COMPOSE_FILE" up -d --force-recreate --scale "api=1" api
if ! docker compose -f "$COMPOSE_FILE" up -d --wait api; then
  echo "API failed health checks. Recent API logs:" >&2
  docker compose -f "$COMPOSE_FILE" logs --tail=200 api >&2 || true
  exit 1
fi

if [[ -x scripts/once-copy-june-photo-profiles.sh ]]; then
  echo "==> One-time photo profile season migration..."
  ./scripts/once-copy-june-photo-profiles.sh
fi

if [[ "$API_REPLICAS" -gt 1 ]]; then
  echo "==> API: scale to ${API_REPLICAS} replicas..."
  docker compose -f "$COMPOSE_FILE" up -d --force-recreate --scale "api=${API_REPLICAS}" api
  if ! docker compose -f "$COMPOSE_FILE" up -d --wait api; then
    echo "API failed health checks after scale. Recent API logs:" >&2
    docker compose -f "$COMPOSE_FILE" logs --tail=200 api >&2 || true
    exit 1
  fi
fi

echo "==> Remaining services..."
docker compose -f "$COMPOSE_FILE" up -d --build \
  api-gateway bot ticket-admin-seed ticket-api ticket-bot tgifts-buyer

echo "==> Status"
docker compose -f "$COMPOSE_FILE" ps

echo
echo "Health: curl -fsS http://127.0.0.1:\${API_GATEWAY_PORT:-8080}/health/live"
