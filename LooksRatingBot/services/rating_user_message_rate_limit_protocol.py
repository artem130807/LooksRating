from __future__ import annotations

from enum import Enum
from typing import Protocol


class RatingMessageRateLimitBlock(Enum):
    PAIR = "pair"
    SENDER = "sender"


class RatingUserMessageRateLimiter(Protocol):
    async def check_delivery(
        self,
        *,
        sender_telegram_id: int,
        recipient_telegram_id: int,
    ) -> RatingMessageRateLimitBlock | None: ...

    async def record_delivery(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None: ...


class AllowAllRatingUserMessageRateLimiter:
    async def check_delivery(
        self,
        *,
        sender_telegram_id: int,
        recipient_telegram_id: int,
    ) -> RatingMessageRateLimitBlock | None:
        return None

    async def record_delivery(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        return None
