from __future__ import annotations


def build_writing_off_sparks_idempotency_key(*, telegram_id: int, callback_id: str) -> str:
    """Stable idempotency key for a single Telegram callback confirmation."""
    normalized_callback_id = callback_id.strip()
    return f"writing-off-sparks:{telegram_id}:{normalized_callback_id}"
