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

    async def try_acquire(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool:
        now = time.monotonic()
        sender_bucket = self._touch_bucket(
            self._sender_buckets,
            str(sender_telegram_id),
            now,
        )
        if sender_bucket.count >= self._sender_limit:
            return False

        pair_key = pair_rate_key(sender_telegram_id, recipient_telegram_id)
        pair_bucket = self._touch_bucket(self._pair_buckets, pair_key, now)
        if pair_bucket.count >= self._pair_limit:
            return False

        sender_bucket.count += 1
        pair_bucket.count += 1
        return True

    async def release(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        now = time.monotonic()
        self._decrement_bucket(self._sender_buckets, str(sender_telegram_id), now)
        self._decrement_bucket(
            self._pair_buckets,
            pair_rate_key(sender_telegram_id, recipient_telegram_id),
            now,
        )

    def _decrement_bucket(
        self,
        buckets: dict[str, _RateBucket],
        key: str,
        now: float,
    ) -> None:
        bucket = buckets.get(key)
        if bucket is None or bucket.expires_at_monotonic <= now:
            return
        if bucket.count > 0:
            bucket.count -= 1
        if bucket.count <= 0:
            buckets.pop(key, None)

    def _touch_bucket(
        self,
        buckets: dict[str, _RateBucket],
        key: str,
        now: float,
    ) -> _RateBucket:
        bucket = buckets.get(key)
        if bucket is None or bucket.expires_at_monotonic <= now:
            bucket = _RateBucket(count=0, expires_at_monotonic=now + self._window_seconds)
            buckets[key] = bucket
        return bucket
