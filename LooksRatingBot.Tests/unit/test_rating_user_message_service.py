from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock

import pytest

from bot import texts
from services.rating_user_message_service import (
    RatingUserMessageService,
    resolve_sender_display_name,
    validate_message_text,
)
from services.in_memory_rating_user_message_store import InMemoryRatingUserMessageStore
from services.in_memory_rating_user_message_rate_limiter import InMemoryRatingUserMessageRateLimiter


@pytest.mark.parametrize(
    ("text", "expected"),
    [
        ("Привет", "Привет"),
        ("  trimmed  ", "trimmed"),
        ("", None),
        ("   ", None),
        ("x" * 500, "x" * 500),
        ("x" * 501, None),
    ],
)
def test_validate_message_text(text: str, expected: str | None) -> None:
    assert validate_message_text(text) == expected


@pytest.mark.asyncio
async def test_resolve_sender_display_name_prefers_profile_display_name() -> None:
    api = MagicMock()
    api.get_user = AsyncMock(return_value={"displayName": "Катя"})

    result = await resolve_sender_display_name(
        api,
        sender_telegram_id=10_001,
        fallback_username="ignored",
    )

    assert result == "Катя"


@pytest.mark.asyncio
async def test_resolve_sender_display_name_falls_back_to_username() -> None:
    api = MagicMock()
    api.get_user = AsyncMock(return_value={"telegramUsername": "katya"})

    result = await resolve_sender_display_name(
        api,
        sender_telegram_id=10_001,
        fallback_username=None,
    )

    assert result == "@katya"


@pytest.mark.asyncio
async def test_send_message_delivers_notification() -> None:
    bot = MagicMock()
    bot.send_message = AsyncMock()
    api = MagicMock()
    api.get_user = AsyncMock(return_value={"displayName": "Иван"})
    store = InMemoryRatingUserMessageStore()
    service = RatingUserMessageService(bot, store)

    success, message = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_001,
        text="Классное фото!",
    )

    assert success is True
    assert message == "Сообщение отправлено."
    bot.send_message.assert_awaited_once()
    assert bot.send_message.await_args.kwargs["chat_id"] == 20_001
    notification_text = bot.send_message.await_args.kwargs["text"]
    assert notification_text == texts.RATING_MESSAGE_RECEIVED_NOTIFICATION
    assert "Иван" not in notification_text
    callback_data = bot.send_message.await_args.kwargs["reply_markup"].inline_keyboard[0][0].callback_data
    token = callback_data.removeprefix("rms:sh:")
    stored = await store.get(token)
    assert stored is not None
    assert stored.text == "Классное фото!"


@pytest.mark.asyncio
async def test_send_message_rejects_self_message() -> None:
    bot = MagicMock()
    bot.send_message = AsyncMock()
    api = MagicMock()
    service = RatingUserMessageService(bot, InMemoryRatingUserMessageStore())

    success, message = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=10_001,
        text="Самому себе",
    )

    assert success is False
    assert "самому себе" in message.lower()
    bot.send_message.assert_not_called()


@pytest.mark.asyncio
async def test_send_message_rejects_when_rate_limited() -> None:
    bot = MagicMock()
    bot.send_message = AsyncMock()
    api = MagicMock()
    api.get_user = AsyncMock(return_value={"displayName": "Иван"})
    rate_limiter = InMemoryRatingUserMessageRateLimiter(
        sender_limit=10,
        pair_limit=1,
        window_seconds=3600,
    )
    service = RatingUserMessageService(
        bot,
        InMemoryRatingUserMessageStore(),
        rate_limiter,
    )

    first = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_001,
        text="Первое",
    )
    second = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_001,
        text="Второе",
    )

    assert first[0] is True
    assert second[0] is False
    assert "этого участника" in second[1].lower()
    assert bot.send_message.await_count == 1


@pytest.mark.asyncio
async def test_send_message_rejects_when_sender_rate_limited() -> None:
    bot = MagicMock()
    bot.send_message = AsyncMock()
    api = MagicMock()
    api.get_user = AsyncMock(return_value={"displayName": "Иван"})
    rate_limiter = InMemoryRatingUserMessageRateLimiter(
        sender_limit=1,
        pair_limit=10,
        window_seconds=3600,
    )
    service = RatingUserMessageService(
        bot,
        InMemoryRatingUserMessageStore(),
        rate_limiter,
    )

    first = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_001,
        text="Первое",
    )
    second = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_002,
        text="Второе",
    )

    assert first[0] is True
    assert second[0] is False
    assert "личных сообщений" in second[1].lower()


@pytest.mark.asyncio
async def test_get_pending_reads_from_store() -> None:
    bot = MagicMock()
    store = InMemoryRatingUserMessageStore()
    service = RatingUserMessageService(bot, store)
    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Текст",
    )

    pending = await service.get_pending(saved.token)
    assert pending is not None
    assert pending.text == "Текст"


@pytest.mark.asyncio
async def test_get_pending_for_recipient_rejects_wrong_user() -> None:
    bot = MagicMock()
    store = InMemoryRatingUserMessageStore()
    service = RatingUserMessageService(bot, store)
    saved = await store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Анна",
        text="Текст",
    )

    pending = await service.get_pending_for_recipient(
        saved.token,
        recipient_telegram_id=99_999,
    )
    assert pending is None


@pytest.mark.asyncio
async def test_send_message_does_not_count_rate_limit_on_delivery_failure() -> None:
    from aiogram.exceptions import TelegramForbiddenError

    bot = MagicMock()
    bot.send_message = AsyncMock(
        side_effect=[
            TelegramForbiddenError(method="sendMessage", message="blocked"),
            None,
        ],
    )
    api = MagicMock()
    api.get_user = AsyncMock(return_value={"displayName": "Иван"})
    rate_limiter = InMemoryRatingUserMessageRateLimiter(
        sender_limit=1,
        pair_limit=1,
        window_seconds=3600,
    )
    service = RatingUserMessageService(
        bot,
        InMemoryRatingUserMessageStore(),
        rate_limiter,
    )

    first = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_001,
        text="Первое",
    )
    second = await service.send_message(
        api,
        sender_telegram_id=10_001,
        sender_username="ivan",
        recipient_telegram_id=20_001,
        text="Второе",
    )

    assert first[0] is False
    assert second[0] is True
