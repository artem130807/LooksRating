#!/usr/bin/env bash
# Deploy LooksRating to VPS using GitHub Actions secrets (environment: production).
# Required env: SSH_PRIVATE_KEY, VPS_HOST, VPS_USER, TELEGRAM_BOT_TOKEN, API_KEY,
# POSTGRES_PASSWORD, TICKET_BOT_TOKEN, TICKET_API_KEY, TICKET_POSTGRES_PASSWORD,
# TGIFTS_API_ID, TGIFTS_API_HASH

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
bash "$SCRIPT_DIR/validate-deploy-secrets.sh"

if [[ -n "${VPS_APP_DIR:-}" ]]; then
  APP_DIR="$VPS_APP_DIR"
elif [[ "${VPS_USER}" == "root" ]]; then
  APP_DIR="/root/LooksRating"
else
  APP_DIR="/home/${VPS_USER}/LooksRating"
fi
GIT_REF="${DEPLOY_GIT_REF:-main}"
SSH_OPTS=(-o StrictHostKeyChecking=accept-new -o BatchMode=yes)

WORKDIR="$(mktemp -d)"
trap 'rm -rf "$WORKDIR"' EXIT
umask 077

KEY_FILE="$WORKDIR/deploy_key"
printf '%s\n' "$SSH_PRIVATE_KEY" > "$KEY_FILE"
chmod 600 "$KEY_FILE"

cat > "$WORKDIR/.env" <<EOF
TELEGRAM_BOT_TOKEN=${TELEGRAM_BOT_TOKEN}
API_KEY=${API_KEY}
POSTGRES_PASSWORD=${POSTGRES_PASSWORD}
TICKET_BOT_TOKEN=${TICKET_BOT_TOKEN}
TICKET_API_KEY=${TICKET_API_KEY}
TICKET_POSTGRES_PASSWORD=${TICKET_POSTGRES_PASSWORD}
COMPOSE_PROJECT_NAME=${COMPOSE_PROJECT_NAME:-looksrating}
EOF

cat > "$WORKDIR/tgifts-buyer.env" <<EOF
API_ID=${TGIFTS_API_ID}
API_HASH=${TGIFTS_API_HASH}
APP_MODE=${TGIFTS_APP_MODE:-gift_grpc}
VIP_GIFT_JOB_ENABLED=true
VIP_GIFT_INTERVAL_DAYS=14
STARTUP_GIFT_DISPATCH=false
VIP_GIFT_SEND_INTRO=false
INTERVAL=10
TIMEZONE=Europe/Moscow
CHANNEL_ID=${TGIFTS_CHANNEL_ID:-0}
LANGUAGE=RU
MIN_GIFT_PRICE=0
MAX_GIFT_PRICE=10000
GIFT_DELAY=5
NUM_GIFTS=1
HIDE_SENDER_NAME=True
PURCHASE_NON_LIMITED_GIFTS=False
LOOKSRATING_GRPC_ADDRESS=api:8081
LOOKSRATING_GRPC_TIMEOUT=120
USE_LOOKSRATING_GRPC=false
GIFT_GRPC_ENABLED=true
GIFT_GRPC_HOST=0.0.0.0
GIFT_GRPC_PORT=50051
EOF

REMOTE="${VPS_USER}@${VPS_HOST}"

echo "==> Upload env files"
ssh -i "$KEY_FILE" "${SSH_OPTS[@]}" "$REMOTE" "mkdir -p '$APP_DIR/TGgifts-buyer'"
scp -i "$KEY_FILE" "${SSH_OPTS[@]}" "$WORKDIR/.env" "$REMOTE:$APP_DIR/.env"
scp -i "$KEY_FILE" "${SSH_OPTS[@]}" "$WORKDIR/tgifts-buyer.env" "$REMOTE:$APP_DIR/TGgifts-buyer/.env"

echo "==> Pull latest code and restart stack"
ssh -i "$KEY_FILE" "${SSH_OPTS[@]}" "$REMOTE" bash -s <<REMOTE_SCRIPT
set -euo pipefail
APP_DIR='$APP_DIR'
GIT_REF='$GIT_REF'

if [[ ! -d "\$APP_DIR/.git" ]]; then
  echo "Repository not found at \$APP_DIR — clone it on the server first." >&2
  exit 1
fi

cd "\$APP_DIR"
git fetch origin "\$GIT_REF"
git checkout "\$GIT_REF"
git pull --ff-only origin "\$GIT_REF"

chmod 600 .env TGgifts-buyer/.env
chmod +x scripts/prod-up.sh scripts/prod-down.sh scripts/deploy-via-ssh.sh scripts/once-copy-june-photo-profiles.sh 2>/dev/null || true

./scripts/prod-up.sh

echo "==> Health check"
curl -fsS http://127.0.0.1:\${API_GATEWAY_PORT:-8080}/health/live
echo
docker compose -f docker-compose.prod.yml ps
REMOTE_SCRIPT

echo "Deploy completed successfully."
