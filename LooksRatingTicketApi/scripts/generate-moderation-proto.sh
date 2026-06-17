#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
CONTRACTS_PROTO="${ROOT}/../LooksRating.Contracts/Proto"
OUT_DIR="${ROOT}/internal/gen/looksratingpb"

mkdir -p "$OUT_DIR/proto"
cp "$CONTRACTS_PROTO"/remove_tickets_photoprofile.proto "$OUT_DIR/proto/"
cp "$CONTRACTS_PROTO"/reject_ticket_photoprofile.proto "$OUT_DIR/proto/"

protoc -I "$OUT_DIR/proto" \
  --go_out="$OUT_DIR" --go_opt=paths=source_relative \
  --go-grpc_out="$OUT_DIR" --go-grpc_opt=paths=source_relative \
  "$OUT_DIR/proto/remove_tickets_photoprofile.proto" \
  "$OUT_DIR/proto/reject_ticket_photoprofile.proto"

echo "proto generated in internal/gen/looksratingpb"
