from __future__ import annotations

import pytest

from services.in_memory_rating_user_message_rate_limiter import InMemoryRatingUserMessageRateLimiter


@pytest.mark.asyncio
async def test_allows_messages_up_to_pair_limit() -> None:
    limiter = InMemoryRatingUserMessageRateLimiter(
        sender_limit=10,
        pair_limit=3,
        window_seconds=3600,
    )

    for _ in range(3):
        assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_001) is True
        await limiter.record_delivery(sender_telegram_id=10_001, recipient_telegram_id=20_001)

    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_001) is False


@pytest.mark.asyncio
async def test_allows_messages_to_different_recipients_within_sender_limit() -> None:
    limiter = InMemoryRatingUserMessageRateLimiter(
        sender_limit=3,
        pair_limit=2,
        window_seconds=3600,
    )

    for recipient_id in (20_001, 20_002, 20_003):
        assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=recipient_id) is True
        await limiter.record_delivery(sender_telegram_id=10_001, recipient_telegram_id=recipient_id)

    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_004) is False


@pytest.mark.asyncio
async def test_rejected_checks_do_not_consume_quota() -> None:
    limiter = InMemoryRatingUserMessageRateLimiter(
        sender_limit=1,
        pair_limit=1,
        window_seconds=3600,
    )

    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_001) is True
    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_001) is True

    await limiter.record_delivery(sender_telegram_id=10_001, recipient_telegram_id=20_001)

    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_001) is False


@pytest.mark.asyncio
async def test_redis_rate_limiter_blocks_after_sender_limit() -> None:
    pytest.importorskip("fakeredis")
    import fakeredis.aioredis

    from services.redis_rating_user_message_rate_limiter import RedisRatingUserMessageRateLimiter

    redis_client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    limiter = RedisRatingUserMessageRateLimiter(
        redis_client,
        sender_limit=2,
        pair_limit=5,
        window_seconds=3600,
    )

    for recipient_id in (20_001, 20_002):
        assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=recipient_id) is True
        await limiter.record_delivery(sender_telegram_id=10_001, recipient_telegram_id=recipient_id)

    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_003) is False


@pytest.mark.asyncio
async def test_redis_rate_limiter_repairs_keys_without_ttl() -> None:
    pytest.importorskip("fakeredis")
    import fakeredis.aioredis

    from services.rating_user_message_rate_limit_keys import sender_rate_key
    from services.redis_rating_user_message_rate_limiter import RedisRatingUserMessageRateLimiter

    redis_client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    stale_key = sender_rate_key(10_001)
    await redis_client.set(stale_key, "2")

    limiter = RedisRatingUserMessageRateLimiter(
        redis_client,
        sender_limit=15,
        pair_limit=3,
        window_seconds=3600,
    )

    assert await limiter.is_allowed(sender_telegram_id=10_001, recipient_telegram_id=20_001) is True
    ttl = await redis_client.ttl(stale_key)
    assert ttl > 0
