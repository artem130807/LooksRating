#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROTO_SRC="${ROOT}/../LooksRating.Contracts/Proto/admin_tickets.proto"

cp "$PROTO_SRC" "$ROOT/proto/admin_tickets.proto"

protoc -I "$ROOT/proto" \
  --go_out="$ROOT/internal/gen/ticketspb" --go_opt=paths=source_relative \
  --go-grpc_out="$ROOT/internal/gen/ticketspb" --go-grpc_opt=paths=source_relative \
  "$ROOT/proto/admin_tickets.proto"

echo "proto generated in internal/gen/ticketspb"
