from __future__ import annotations

from typing import Protocol

from services.rating_user_message_models import PendingRatingUserMessage


class RatingUserMessageStore(Protocol):
    async def save(
        self,
        *,
        recipient_telegram_id: int,
        sender_telegram_id: int,
        sender_display_name: str,
        text: str,
    ) -> PendingRatingUserMessage: ...

    async def get(self, token: str) -> PendingRatingUserMessage | None: ...

    async def remove(self, token: str) -> None: ...
