from __future__ import annotations

from uuid import uuid4

from services.rating_user_message_models import PendingRatingUserMessage


class InMemoryRatingUserMessageStore:
    """Test/dev fallback store for rating relay messages."""

    def __init__(self) -> None:
        self._messages: dict[str, PendingRatingUserMessage] = {}

    async def save(
        self,
        *,
        recipient_telegram_id: int,
        sender_telegram_id: int,
        sender_display_name: str,
        text: str,
    ) -> PendingRatingUserMessage:
        token = uuid4().hex
        message = PendingRatingUserMessage(
            token=token,
            recipient_telegram_id=recipient_telegram_id,
            sender_telegram_id=sender_telegram_id,
            sender_display_name=sender_display_name.strip() or "Участник",
            text=text.strip(),
        )
        self._messages[token] = message
        return message

    async def get(self, token: str) -> PendingRatingUserMessage | None:
        normalized = (token or "").strip()
        if not normalized:
            return None
        return self._messages.get(normalized)

    async def remove(self, token: str) -> None:
        normalized = (token or "").strip()
        if normalized:
            self._messages.pop(normalized, None)

    async def clear(self) -> None:
        self._messages.clear()
