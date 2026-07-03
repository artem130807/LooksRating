from __future__ import annotations

import asyncio
from unittest.mock import AsyncMock, MagicMock

import pytest

from bot import texts
from config import Settings
from services.inactive_users_reengagement import InactiveUsersReengagementService


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
        channel_promo_interval_seconds=86_400,
        channel_promo_page_size=100,
        channel_promo_send_delay_seconds=0.05,
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


@pytest.mark.asyncio
async def test_tick_sends_in_batches_with_pause_between_batches(
    monkeypatch: pytest.MonkeyPatch,
) -> None:
    bot = AsyncMock()
    service = InactiveUsersReengagementService(_settings(), bot)
    service._grpc = MagicMock()
    service._grpc.get_unactive_users.return_value = [1001, 1002, 1003, 1004, 1005]
    service._BATCH_SIZE = 2
    service._BATCH_PAUSE_SECONDS = 300

    pause_calls: list[float] = []

    async def _fake_wait_for(awaitable, timeout: float):  # type: ignore[no-untyped-def]
        pause_calls.append(timeout)
        awaitable.close()
        raise asyncio.TimeoutError

    monkeypatch.setattr("services.inactive_users_reengagement.asyncio.wait_for", _fake_wait_for)

    await service._tick()

    assert bot.send_message.await_count == 5
    assert pause_calls == [300, 300]


@pytest.mark.asyncio
async def test_tick_deduplicates_and_skips_invalid_telegram_ids() -> None:
    bot = AsyncMock()
    service = InactiveUsersReengagementService(_settings(), bot)
    service._grpc = MagicMock()
    service._grpc.get_unactive_users.return_value = [0, -1, 2001, 2001, 2002]

    await service._tick()

    assert bot.send_message.await_count == 2
    bot.send_message.assert_any_await(
        chat_id=2001,
        text=texts.INACTIVE_USERS_REENGAGEMENT_TEXT,
        disable_web_page_preview=True,
    )
    bot.send_message.assert_any_await(
        chat_id=2002,
        text=texts.INACTIVE_USERS_REENGAGEMENT_TEXT,
        disable_web_page_preview=True,
    )


@pytest.mark.asyncio
async def test_tick_does_nothing_when_grpc_returns_empty_list() -> None:
    bot = AsyncMock()
    service = InactiveUsersReengagementService(_settings(), bot)
    service._grpc = MagicMock()
    service._grpc.get_unactive_users.return_value = []

    await service._tick()

    bot.send_message.assert_not_awaited()


@pytest.mark.asyncio
async def test_run_waits_before_first_tick() -> None:
    bot = AsyncMock()
    service = InactiveUsersReengagementService(_settings(), bot)
    service._RUN_INTERVAL_SECONDS = 60
    service._tick = AsyncMock()

    task = asyncio.create_task(service._run())
    await asyncio.sleep(0.05)
    service._stop_event.set()
    await task

    service._tick.assert_not_awaited()
