from __future__ import annotations

import json
from typing import Any

from services.rating_user_message_models import PendingRatingUserMessage


def serialize_message(message: PendingRatingUserMessage) -> str:
    payload = {
        "token": message.token,
        "recipientTelegramId": message.recipient_telegram_id,
        "senderTelegramId": message.sender_telegram_id,
        "senderDisplayName": message.sender_display_name,
        "text": message.text,
    }
    return json.dumps(payload, ensure_ascii=False, separators=(",", ":"))


def deserialize_message(raw: str | bytes | None) -> PendingRatingUserMessage | None:
    if raw is None:
        return None
    if isinstance(raw, bytes):
        raw = raw.decode("utf-8")
    normalized = raw.strip()
    if not normalized:
        return None

    try:
        payload: dict[str, Any] = json.loads(normalized)
    except json.JSONDecodeError:
        return None

    token = str(payload.get("token") or "").strip()
    recipient_telegram_id = int(payload.get("recipientTelegramId") or 0)
    sender_telegram_id = int(payload.get("senderTelegramId") or 0)
    sender_display_name = str(payload.get("senderDisplayName") or "").strip() or "Участник"
    text = str(payload.get("text") or "").strip()

    if not token or recipient_telegram_id <= 0 or sender_telegram_id <= 0 or not text:
        return None

    return PendingRatingUserMessage(
        token=token,
        recipient_telegram_id=recipient_telegram_id,
        sender_telegram_id=sender_telegram_id,
        sender_display_name=sender_display_name,
        text=text,
    )
