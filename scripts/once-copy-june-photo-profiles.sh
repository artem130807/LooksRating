#!/usr/bin/env bash
# One-time deploy helper: verifies that June→July photo profile migration ran on API startup,
# then removes this script from disk.
set -euo pipefail

SCRIPT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/$(basename "${BASH_SOURCE[0]}")"
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

COMPOSE_FILE="docker-compose.prod.yml"
MIGRATION_NAME="copy-photo-profiles:2c081626-23e1-4740-a871-fac8a97519be:93ee80fe-cae5-4e44-8e03-d8eea253acb9"

if [[ ! -f "$COMPOSE_FILE" ]]; then
  echo "Compose file not found: $COMPOSE_FILE" >&2
  exit 1
fi

echo "==> Verifying API container health (migration runs during API startup)..."
docker compose -f "$COMPOSE_FILE" exec -T api curl -fsS http://localhost:8080/health/ready >/dev/null

echo "==> Verifying deploy migration in database..."
RESULT="$(docker compose -f "$COMPOSE_FILE" exec -T postgres \
  psql -U postgres -d LooksRatingDb -tAc \
  "SELECT 1 FROM \"DeployMigrationHistory\" WHERE \"Name\" = '${MIGRATION_NAME}';")"

if [[ "$(echo "$RESULT" | tr -d '[:space:]')" != "1" ]]; then
  echo "Deploy migration not found in DeployMigrationHistory: ${MIGRATION_NAME}" >&2
  echo "Check API logs: docker compose -f ${COMPOSE_FILE} logs api --tail 200" >&2
  exit 1
fi

echo "==> Migration verified. Removing one-time script."
rm -f "$SCRIPT_PATH"
