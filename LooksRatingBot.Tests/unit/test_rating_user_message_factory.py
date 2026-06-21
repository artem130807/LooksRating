from __future__ import annotations

from unittest.mock import MagicMock

import pytest
import fakeredis.aioredis

from config import Settings
from services.in_memory_rating_user_message_rate_limiter import InMemoryRatingUserMessageRateLimiter
from services.in_memory_rating_user_message_store import InMemoryRatingUserMessageStore
from services.rating_user_message_factory import (
    build_rating_user_message_rate_limiter,
    build_rating_user_message_store,
)
from services.redis_rating_user_message_rate_limiter import RedisRatingUserMessageRateLimiter
from services.redis_rating_user_message_store import RedisRatingUserMessageStore


def _settings(*, redis_url: str | None) -> Settings:
    return Settings(
        bot_token="token",
        api_base_url="http://api:8080",
        api_grpc_address="api:8081",
        tgifts_grpc_address="tgifts:50051",
        grpc_timeout_seconds=30.0,
        api_key="key",
        telegram_proxy=None,
        top_notify_interval_seconds=60,
        review_notify_interval_seconds=60,
        internal_notify_host="0.0.0.0",
        internal_notify_port=8092,
        internal_notify_api_key="key",
        stars_provider_token="",
        channel_username="LooksRatingBotOfficial",
        channel_url="https://t.me/LooksRatingBotOfficial",
        channel_promo_interval_seconds=7200,
        channel_promo_page_size=100,
        channel_promo_send_delay_seconds=0.05,
        channel_promo_enabled=True,
        redis_url=redis_url,
        rating_message_ttl_seconds=604800,
        rating_message_sender_limit_per_window=15,
        rating_message_pair_limit_per_window=20,
        rating_message_rate_limit_window_seconds=3600,
    )


def test_factory_uses_redis_when_client_available() -> None:
    redis_client = MagicMock()
    store = build_rating_user_message_store(_settings(redis_url="redis://redis:6379/0"), redis_client)
    assert isinstance(store, RedisRatingUserMessageStore)


def test_factory_falls_back_to_memory_without_redis_url() -> None:
    store = build_rating_user_message_store(_settings(redis_url=None), None)
    assert isinstance(store, InMemoryRatingUserMessageStore)


@pytest.mark.asyncio
async def test_factory_redis_store_is_functional() -> None:
    redis_client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    store = build_rating_user_message_store(_settings(redis_url="redis://redis:6379/0"), redis_client)
    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="ok",
    )
    loaded = await store.get(saved.token)
    assert loaded is not None


def test_rate_limiter_factory_uses_redis_when_client_available() -> None:
    redis_client = MagicMock()
    limiter = build_rating_user_message_rate_limiter(
        _settings(redis_url="redis://redis:6379/0"),
        redis_client,
    )
    assert isinstance(limiter, RedisRatingUserMessageRateLimiter)


def test_rate_limiter_factory_falls_back_to_memory_without_redis_url() -> None:
    limiter = build_rating_user_message_rate_limiter(_settings(redis_url=None), None)
    assert isinstance(limiter, InMemoryRatingUserMessageRateLimiter)
