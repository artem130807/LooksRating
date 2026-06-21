from __future__ import annotations

from typing import Protocol


class RatingUserMessageRateLimiter(Protocol):
    async def is_allowed(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool: ...

    async def record_delivery(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None: ...


class AllowAllRatingUserMessageRateLimiter:
    async def is_allowed(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool:
        return True

    async def record_delivery(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        return None
