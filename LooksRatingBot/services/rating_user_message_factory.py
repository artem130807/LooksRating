from __future__ import annotations

import logging

import redis.asyncio as redis

from config import Settings
from services.in_memory_rating_user_message_rate_limiter import InMemoryRatingUserMessageRateLimiter
from services.in_memory_rating_user_message_store import InMemoryRatingUserMessageStore
from services.rating_user_message_protocol import RatingUserMessageStore
from services.rating_user_message_rate_limit_protocol import RatingUserMessageRateLimiter
from services.redis_rating_user_message_rate_limiter import RedisRatingUserMessageRateLimiter
from services.redis_rating_user_message_store import RedisRatingUserMessageStore

logger = logging.getLogger(__name__)


def build_rating_user_message_store(
    settings: Settings,
    redis_client: redis.Redis | None,
) -> RatingUserMessageStore:
    if settings.redis_enabled and redis_client is not None:
        return RedisRatingUserMessageStore(
            redis_client,
            ttl_seconds=settings.rating_message_ttl_seconds,
        )

    logger.warning(
        "Rating user messages use in-memory store (REDIS_URL is not configured). "
        "Messages will be lost on bot restart."
    )
    return InMemoryRatingUserMessageStore()


def build_rating_user_message_rate_limiter(
    settings: Settings,
    redis_client: redis.Redis | None,
) -> RatingUserMessageRateLimiter:
    if settings.redis_enabled and redis_client is not None:
        return RedisRatingUserMessageRateLimiter(
            redis_client,
            sender_limit=settings.rating_message_sender_limit_per_window,
            pair_limit=settings.rating_message_pair_limit_per_window,
            window_seconds=settings.rating_message_rate_limit_window_seconds,
        )

    if settings.redis_enabled:
        logger.warning(
            "Rating message rate limiter uses in-memory fallback because Redis is unavailable."
        )
    return InMemoryRatingUserMessageRateLimiter(
        sender_limit=settings.rating_message_sender_limit_per_window,
        pair_limit=settings.rating_message_pair_limit_per_window,
        window_seconds=settings.rating_message_rate_limit_window_seconds,
    )
