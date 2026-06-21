from __future__ import annotations

API_KEY_METADATA_HEADER = "x-api-key"


def build_grpc_metadata(api_key: str) -> tuple[tuple[str, str], ...]:
    normalized = (api_key or "").strip()
    if not normalized:
        return ()
    return ((API_KEY_METADATA_HEADER, normalized),)
