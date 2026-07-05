from __future__ import annotations

from unittest.mock import AsyncMock

import pytest
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from bot import texts
from bot.keyboards import channel_subscribe_keyboard
from config import Settings
from services.channel_subscribe_promo_delivery import (
    build_channel_subscribe_promo_text,
    send_channel_subscribe_promo,
)


def _settings(**overrides) -> Settings:
    defaults = dict(
        bot_token="token",
        api_base_url="http://api:8080",
        api_grpc_address="api:8080",
        tgifts_grpc_address="tgifts:50051",
        grpc_timeout_seconds=30.0,
        api_key="key",
        telegram_proxy=None,
        top_notify_interval_seconds=60,
        review_notify_interval_seconds=60,
        season_rollover_notify_interval_seconds=60,
        stars_provider_token="",
        channel_username="LooksRatingBotOfficial",
        channel_url="https://t.me/LooksRatingBotOfficial",
        channel_promo_interval_seconds=7200,
        channel_promo_page_size=100,
        channel_promo_send_delay_seconds=0.0,
        channel_promo_enabled=True,
        internal_notify_host="0.0.0.0",
        internal_notify_port=8092,
        internal_notify_api_key="test-key",
        redis_url="redis://redis:6379/0",
        rating_message_ttl_seconds=604800,
        rating_message_sender_limit_per_window=15,
        rating_message_pair_limit_per_window=20,
        rating_message_rate_limit_window_seconds=3600,
    )
    defaults.update(overrides)
    return Settings(**defaults)


def test_build_channel_subscribe_promo_text_uses_settings_url() -> None:
    settings = _settings(channel_url="https://t.me/CustomChannel")

    text = build_channel_subscribe_promo_text(settings)

    assert "https://t.me/CustomChannel" in text
    assert text.startswith("📢")


@pytest.mark.asyncio
async def test_send_channel_subscribe_promo_sends_expected_message() -> None:
    settings = _settings()
    bot = AsyncMock()

    sent = await send_channel_subscribe_promo(bot, settings, 42_001)

    assert sent is True
    bot.send_message.assert_awaited_once_with(
        chat_id=42_001,
        text=build_channel_subscribe_promo_text(settings),
        reply_markup=channel_subscribe_keyboard(),
        disable_web_page_preview=False,
    )


@pytest.mark.asyncio
@pytest.mark.parametrize("exc", [TelegramForbiddenError(method="sendMessage", message="blocked"), TelegramBadRequest(method="sendMessage", message="bad")])
async def test_send_channel_subscribe_promo_returns_false_on_telegram_errors(exc: Exception) -> None:
    bot = AsyncMock()
    bot.send_message.side_effect = exc

    sent = await send_channel_subscribe_promo(bot, _settings(), 42_001)

    assert sent is False


@pytest.mark.asyncio
async def test_send_channel_subscribe_promo_rejects_invalid_telegram_id() -> None:
    bot = AsyncMock()

    sent = await send_channel_subscribe_promo(bot, _settings(), 0)

    assert sent is False
    bot.send_message.assert_not_called()


@pytest.mark.asyncio
async def test_send_channel_subscribe_promo_returns_false_on_unexpected_error() -> None:
    bot = AsyncMock()
    bot.send_message.side_effect = RuntimeError("network down")

    sent = await send_channel_subscribe_promo(bot, _settings(), 42_001)

    assert sent is False
