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

    async def try_acquire(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> bool:
        sender_key = sender_rate_key(sender_telegram_id)
        pair_key = pair_rate_key(sender_telegram_id, recipient_telegram_id)

        sender_count = await self._increment_with_window(sender_key)
        if sender_count > self._sender_limit:
            return False

        pair_count = await self._increment_with_window(pair_key)
        if pair_count > self._pair_limit:
            await self._redis.decr(sender_key)
            return False

        return True

    async def release(self, *, sender_telegram_id: int, recipient_telegram_id: int) -> None:
        await self._decrement_if_positive(sender_rate_key(sender_telegram_id))
        await self._decrement_if_positive(
            pair_rate_key(sender_telegram_id, recipient_telegram_id),
        )

    async def _decrement_if_positive(self, key: str) -> None:
        count = await self._redis.get(key)
        if count is None:
            return
        try:
            current = int(count)
        except (TypeError, ValueError):
            return
        if current <= 1:
            await self._redis.delete(key)
            return
        await self._redis.decr(key)

    async def _increment_with_window(self, key: str) -> int:
        count = int(await self._redis.incr(key))
        if count == 1:
            await self._redis.expire(key, self._window_seconds)
        return count
