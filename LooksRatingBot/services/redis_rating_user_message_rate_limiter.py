from __future__ import annotations

import redis.asyncio as redis

from services.rating_user_message_rate_limit_keys import pair_rate_key, sender_rate_key
from services.rating_user_message_rate_limit_protocol import RatingUserMessageRateLimiter


class RedisRatingUserMessageRateLimiter(RatingUserMessageRateLimiter):
    def __init__(
        self,
        redis_client: redis.Redis,
        *,
        sender_limit: int,
        pair_limit: int,
        window_seconds: int,
    ) -> None:
        if sender_limit <= 0 or pair_limit <= 0 or window_seconds <= 0:
            raise ValueError("rate limit settings must be positive")
        self._redis = redis_client
        self._sender_limit = sender_limit
        self._pair_limit = pair_limit
        self._window_seconds = window_seconds

    async def is_allowed(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool:
        sender_count = await self._read_count(sender_rate_key(sender_telegram_id))
        if sender_count >= self._sender_limit:
            return False

        pair_count = await self._read_count(
            pair_rate_key(sender_telegram_id, recipient_telegram_id),
        )
        return pair_count < self._pair_limit

    async def record_delivery(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        await self._increment_with_window(sender_rate_key(sender_telegram_id))
        await self._increment_with_window(
            pair_rate_key(sender_telegram_id, recipient_telegram_id),
        )

    async def _read_count(self, key: str) -> int:
        raw = await self._redis.get(key)
        if raw is None:
            return 0
        try:
            count = int(raw)
        except (TypeError, ValueError):
            return 0
        if count <= 0:
            return 0

        ttl = await self._redis.ttl(key)
        if ttl == -1:
            await self._redis.expire(key, self._window_seconds)
        return count

    async def _increment_with_window(self, key: str) -> int:
        count = int(await self._redis.incr(key))
        ttl = await self._redis.ttl(key)
        if count == 1 or ttl == -1:
            await self._redis.expire(key, self._window_seconds)
        return count
