from __future__ import annotations

KEY_PREFIX = "looksrating:rating-msg:"


def rating_message_key(token: str) -> str:
    normalized = (token or "").strip()
    return f"{KEY_PREFIX}{normalized}"
