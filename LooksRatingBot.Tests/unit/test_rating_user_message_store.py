from __future__ import annotations

import pytest

from services.in_memory_rating_user_message_store import InMemoryRatingUserMessageStore


@pytest.mark.asyncio
async def test_save_and_get_roundtrip() -> None:
    store = InMemoryRatingUserMessageStore()
    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Привет!",
    )

    loaded = await store.get(saved.token)
    assert loaded is not None
    assert loaded.recipient_telegram_id == 20_001
    assert loaded.sender_telegram_id == 10_001
    assert loaded.sender_display_name == "Анна"
    assert loaded.text == "Привет!"


@pytest.mark.asyncio
async def test_remove_deletes_message() -> None:
    store = InMemoryRatingUserMessageStore()
    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Привет!",
    )

    await store.remove(saved.token)
    assert await store.get(saved.token) is None


@pytest.mark.asyncio
async def test_get_returns_none_for_unknown_token() -> None:
    store = InMemoryRatingUserMessageStore()
    assert await store.get("missing") is None
