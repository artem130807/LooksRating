from __future__ import annotations

KEY_PREFIX = "looksrating:rating-msg-rate:"


def sender_rate_key(sender_telegram_id: int) -> str:
    return f"{KEY_PREFIX}sender:{sender_telegram_id}"


def pair_rate_key(sender_telegram_id: int, recipient_telegram_id: int) -> str:
    return f"{KEY_PREFIX}pair:{sender_telegram_id}:{recipient_telegram_id}"
