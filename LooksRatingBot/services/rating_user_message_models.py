from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class PendingRatingUserMessage:
    token: str
    recipient_telegram_id: int
    sender_telegram_id: int
    sender_display_name: str
    text: str
