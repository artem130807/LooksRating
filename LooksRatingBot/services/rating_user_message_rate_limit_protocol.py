from __future__ import annotations

from typing import Protocol


class RatingUserMessageRateLimiter(Protocol):
    async def try_acquire(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool: ...

    async def release(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None: ...


class AllowAllRatingUserMessageRateLimiter:
    async def try_acquire(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool:
        return True

    async def release(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        return None
