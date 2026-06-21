from __future__ import annotations

import pytest
import fakeredis.aioredis

from services.rating_user_message_keys import rating_message_key
from services.rating_user_message_serializer import deserialize_message, serialize_message
from services.redis_rating_user_message_store import RedisRatingUserMessageStore
from services.rating_user_message_models import PendingRatingUserMessage


def test_serialize_and_deserialize_roundtrip() -> None:
    message = PendingRatingUserMessage(
        token="abc123",
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Иван",
        text="Привет",
    )

    restored = deserialize_message(serialize_message(message))
    assert restored == message


@pytest.mark.asyncio
async def test_redis_store_persists_message_with_ttl() -> None:
    redis_client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    store = RedisRatingUserMessageStore(redis_client, ttl_seconds=3600)

    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Сообщение",
    )

    loaded = await store.get(saved.token)
    assert loaded == saved

    ttl = await redis_client.ttl(rating_message_key(saved.token))
    assert ttl > 0


@pytest.mark.asyncio
async def test_redis_store_remove_deletes_message() -> None:
    redis_client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    store = RedisRatingUserMessageStore(redis_client, ttl_seconds=3600)

    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Сообщение",
    )

    await store.remove(saved.token)
    assert await store.get(saved.token) is None


@pytest.mark.asyncio
async def test_redis_store_survives_new_store_instance() -> None:
    redis_client = fakeredis.aioredis.FakeRedis(decode_responses=True)
    first_store = RedisRatingUserMessageStore(redis_client, ttl_seconds=3600)
    saved = await first_store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Персистентно",
    )

    second_store = RedisRatingUserMessageStore(redis_client, ttl_seconds=3600)
    loaded = await second_store.get(saved.token)
    assert loaded is not None
    assert loaded.text == "Персистентно"
