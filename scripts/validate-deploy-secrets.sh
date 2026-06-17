#!/usr/bin/env bash
# Validates required deploy secrets/env vars before SSH deploy.
# Usage: export secrets, then: bash scripts/validate-deploy-secrets.sh

set -euo pipefail

REQUIRED_VARS=(
  SSH_PRIVATE_KEY
  VPS_HOST
  VPS_USER
  TELEGRAM_BOT_TOKEN
  API_KEY
  POSTGRES_PASSWORD
  TICKET_BOT_TOKEN
  TICKET_API_KEY
  TICKET_POSTGRES_PASSWORD
  TGIFTS_API_ID
  TGIFTS_API_HASH
)

OPTIONAL_VARS=(
  VPS_APP_DIR
  COMPOSE_PROJECT_NAME
  TGIFTS_APP_MODE
  TGIFTS_CHANNEL_ID
  DEPLOY_GIT_REF
)

missing=()
for var in "${REQUIRED_VARS[@]}"; do
  if [[ -z "${!var:-}" ]]; then
    missing+=("$var")
  fi
done

if [[ ${#missing[@]} -eq 0 ]]; then
  echo "Deploy secrets: all ${#REQUIRED_VARS[@]} required variables are set."
  if [[ -z "${VPS_APP_DIR:-}" ]]; then
    if [[ "${VPS_USER:-}" == "root" ]]; then
      echo "VPS_APP_DIR not set — will use default /root/LooksRating for root user."
    else
      echo "VPS_APP_DIR not set — will use default /home/${VPS_USER}/LooksRating."
    fi
  fi
  exit 0
fi

echo "::error::Deploy cannot run: ${#missing[@]} required secret(s) are missing." >&2
echo "" >&2
echo "Add them in GitHub:" >&2
echo "  Settings → Secrets and variables → Actions → Repository secrets" >&2
echo "  or Settings → Environments → production → Environment secrets" >&2
echo "" >&2
echo "Missing required secrets:" >&2
for var in "${missing[@]}"; do
  echo "  ✗ $var" >&2
done
echo "" >&2
echo "Optional (have defaults if omitted):" >&2
for var in "${OPTIONAL_VARS[@]}"; do
  if [[ -z "${!var:-}" ]]; then
    echo "  ○ $var" >&2
  else
    echo "  ✓ $var" >&2
  fi
done
echo "" >&2
echo "Tip: SSH_PRIVATE_KEY must be the full private key (BEGIN/END lines), not the .pub file." >&2
echo "Tip: set VPS_APP_DIR explicitly if the repo lives elsewhere (e.g. /opt/LooksRating)." >&2
echo "Tip: values must be under Secrets, not Variables (workflow reads secrets.* only)." >&2

exit 1
