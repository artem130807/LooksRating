from __future__ import annotations

import time
from dataclasses import dataclass

from services.rating_user_message_rate_limit_keys import pair_rate_key, sender_rate_key
from services.rating_user_message_rate_limit_protocol import RatingUserMessageRateLimiter


@dataclass
class _RateBucket:
    count: int
    expires_at_monotonic: float


class InMemoryRatingUserMessageRateLimiter(RatingUserMessageRateLimiter):
    def __init__(
        self,
        *,
        sender_limit: int,
        pair_limit: int,
        window_seconds: int,
    ) -> None:
        if sender_limit <= 0 or pair_limit <= 0 or window_seconds <= 0:
            raise ValueError("rate limit settings must be positive")
        self._sender_limit = sender_limit
        self._pair_limit = pair_limit
        self._window_seconds = window_seconds
        self._sender_buckets: dict[str, _RateBucket] = {}
        self._pair_buckets: dict[str, _RateBucket] = {}

    async def is_allowed(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool:
        now = time.monotonic()
        sender_count = self._read_count(self._sender_buckets, str(sender_telegram_id), now)
        if sender_count >= self._sender_limit:
            return False

        pair_count = self._read_count(
            self._pair_buckets,
            pair_rate_key(sender_telegram_id, recipient_telegram_id),
            now,
        )
        return pair_count < self._pair_limit

    async def record_delivery(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        now = time.monotonic()
        self._increment_bucket(self._sender_buckets, str(sender_telegram_id), now)
        self._increment_bucket(
            self._pair_buckets,
            pair_rate_key(sender_telegram_id, recipient_telegram_id),
            now,
        )

    def _read_count(
        self,
        buckets: dict[str, _RateBucket],
        key: str,
        now: float,
    ) -> int:
        bucket = buckets.get(key)
        if bucket is None or bucket.expires_at_monotonic <= now:
            return 0
        return bucket.count

    def _increment_bucket(
        self,
        buckets: dict[str, _RateBucket],
        key: str,
        now: float,
    ) -> None:
        bucket = buckets.get(key)
        if bucket is None or bucket.expires_at_monotonic <= now:
            bucket = _RateBucket(count=0, expires_at_monotonic=now + self._window_seconds)
            buckets[key] = bucket
        bucket.count += 1
