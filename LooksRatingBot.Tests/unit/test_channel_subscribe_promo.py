from __future__ import annotations

import asyncio
from unittest.mock import AsyncMock, MagicMock

import pytest

from api.grpc_clients import UsersForMessagePage
from config import Settings
from services.channel_subscribe_promo import ChannelSubscribePromoService


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
        stars_provider_token="",
        channel_username="LooksRatingBotOfficial",
        channel_url="https://t.me/LooksRatingBotOfficial",
        channel_promo_interval_seconds=7200,
        channel_promo_page_size=2,
        channel_promo_send_delay_seconds=0.0,
        channel_promo_enabled=True,
    )
    defaults.update(overrides)
    return Settings(**defaults)


@pytest.mark.asyncio
async def test_tick_sends_messages_and_advances_page() -> None:
    settings = _settings()
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)
    service._grpc = MagicMock()
    service._grpc.get_users_for_message.return_value = UsersForMessagePage(
        telegram_ids=[101, 102],
        total_count=4,
        page=1,
        page_size=2,
        has_next_page=True,
    )

    await service._tick()

    assert bot.send_message.await_count == 2
    service._grpc.get_users_for_message.assert_called_once_with(1, 2, only_unsubscribed_channel=True)
    assert service._current_page == 2


@pytest.mark.asyncio
async def test_tick_resets_page_when_no_next_page() -> None:
    settings = _settings()
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)
    service._current_page = 2
    service._grpc = MagicMock()
    service._grpc.get_users_for_message.return_value = UsersForMessagePage(
        telegram_ids=[103, 104],
        total_count=4,
        page=2,
        page_size=2,
        has_next_page=False,
    )

    await service._tick()

    assert service._current_page == 1


@pytest.mark.asyncio
async def test_tick_skips_users_on_cooldown(monkeypatch: pytest.MonkeyPatch) -> None:
    settings = _settings(channel_promo_interval_seconds=7200)
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)
    service._grpc = MagicMock()
    service._grpc.get_users_for_message.return_value = UsersForMessagePage(
        telegram_ids=[101, 102],
        total_count=2,
        page=1,
        page_size=2,
        has_next_page=False,
    )
    monkeypatch.setattr("services.channel_subscribe_promo.time.time", lambda: 10_000.0)
    service._mark_sent(101, now=10_000.0)

    await service._tick()

    assert bot.send_message.await_count == 1
    assert bot.send_message.await_args.kwargs["chat_id"] == 102


@pytest.mark.asyncio
async def test_tick_does_nothing_when_no_recipients() -> None:
    settings = _settings()
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)
    service._grpc = MagicMock()
    service._grpc.get_users_for_message.return_value = UsersForMessagePage(
        telegram_ids=[],
        total_count=0,
        page=1,
        page_size=2,
        has_next_page=False,
    )

    await service._tick()

    bot.send_message.assert_not_called()


@pytest.mark.asyncio
async def test_tick_safe_skips_when_lock_held() -> None:
    settings = _settings()
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)
    service._tick_lock = asyncio.Lock()
    await service._tick_lock.acquire()

    await service._tick_safe()

    bot.send_message.assert_not_called()
    service._tick_lock.release()


@pytest.mark.asyncio
async def test_run_waits_before_first_tick() -> None:
    settings = _settings(channel_promo_interval_seconds=60)
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)
    service._tick_safe = AsyncMock()

    task = asyncio.create_task(service._run())
    await asyncio.sleep(0.05)
    service._stop_event.set()
    await task

    service._tick_safe.assert_not_awaited()


@pytest.mark.asyncio
async def test_start_does_not_run_when_disabled() -> None:
    settings = _settings(channel_promo_enabled=False)
    bot = AsyncMock()
    service = ChannelSubscribePromoService(settings, bot)

    await service.start()

    assert service._task is None
