from __future__ import annotations

import logging
from uuid import uuid4

import redis.asyncio as redis

from services.rating_user_message_keys import rating_message_key
from services.rating_user_message_models import PendingRatingUserMessage
from services.rating_user_message_serializer import deserialize_message, serialize_message

logger = logging.getLogger(__name__)


class RedisRatingUserMessageStore:
    def __init__(
        self,
        redis_client: redis.Redis,
        *,
        ttl_seconds: int,
    ) -> None:
        if ttl_seconds <= 0:
            raise ValueError("ttl_seconds must be positive")
        self._redis = redis_client
        self._ttl_seconds = ttl_seconds

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
        await self._redis.set(
            rating_message_key(token),
            serialize_message(message),
            ex=self._ttl_seconds,
        )
        return message

    async def get(self, token: str) -> PendingRatingUserMessage | None:
        normalized = (token or "").strip()
        if not normalized:
            return None

        raw = await self._redis.get(rating_message_key(normalized))
        message = deserialize_message(raw)
        if message is None and raw is not None:
            logger.warning("Invalid rating message payload in Redis for token=%s", normalized)
        return message

    async def remove(self, token: str) -> None:
        normalized = (token or "").strip()
        if not normalized:
            return
        await self._redis.delete(rating_message_key(normalized))
