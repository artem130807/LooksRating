from __future__ import annotations

from unittest.mock import ANY, AsyncMock, MagicMock

import pytest
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from bot.season_rollover_notifications import SeasonRolloverNotificationsService
from bot import texts


@pytest.mark.asyncio
async def test_season_rollover_service_sends_and_acks() -> None:
    api = MagicMock()
    bot = AsyncMock()
    api.get_pending_season_rollover_notifications = AsyncMock(
        return_value=[
            {
                "eventId": "event-1",
                "closedSeasonId": "closed-1",
                "closedSeasonName": "Потный июнь",
                "closedSeasonNumber": 6,
                "newSeasonId": "new-1",
                "newSeasonName": "Обгоревший июль",
                "newSeasonNumber": 7,
                "recipientTelegramIds": [1001, 1002],
            }
        ]
    )
    api.ack_season_rollover_notification = AsyncMock()

    service = SeasonRolloverNotificationsService(api=api, bot=bot, interval_seconds=60)
    await service._tick()

    assert bot.send_message.await_count == 2
    bot.send_message.assert_any_call(
        chat_id=1001,
        text=texts.format_season_rollover_notify_text("Потный июнь", "Обгоревший июль"),
        reply_markup=ANY,
    )
    api.ack_season_rollover_notification.assert_any_await(
        event_id="event-1",
        recipient_telegram_ids=[1001],
    )
    api.ack_season_rollover_notification.assert_any_await(
        event_id="event-1",
        recipient_telegram_ids=[1002],
    )


@pytest.mark.asyncio
async def test_season_rollover_service_acks_on_forbidden() -> None:
    api = MagicMock()
    bot = AsyncMock()
    bot.send_message.side_effect = TelegramForbiddenError(
        method="sendMessage",
        message="blocked",
    )
    api.get_pending_season_rollover_notifications = AsyncMock(
        return_value=[
            {
                "eventId": "event-1",
                "closedSeasonId": "closed-1",
                "closedSeasonName": "Потный июнь",
                "newSeasonName": "Обгоревший июль",
                "recipientTelegramIds": [2001],
            }
        ]
    )
    api.ack_season_rollover_notification = AsyncMock()

    service = SeasonRolloverNotificationsService(api=api, bot=bot, interval_seconds=60)
    await service._tick()

    api.ack_season_rollover_notification.assert_awaited_once_with(
        event_id="event-1",
        recipient_telegram_ids=[2001],
    )


@pytest.mark.asyncio
async def test_season_rollover_service_acks_on_bad_request() -> None:
    api = MagicMock()
    bot = AsyncMock()
    bot.send_message.side_effect = TelegramBadRequest(
        method="sendMessage",
        message="invalid",
    )
    api.get_pending_season_rollover_notifications = AsyncMock(
        return_value=[
            {
                "eventId": "event-1",
                "closedSeasonId": "closed-1",
                "closedSeasonName": "Потный июнь",
                "newSeasonName": "Обгоревший июль",
                "recipientTelegramIds": [3001],
            }
        ]
    )
    api.ack_season_rollover_notification = AsyncMock()

    service = SeasonRolloverNotificationsService(api=api, bot=bot, interval_seconds=60)
    await service._tick()

    api.ack_season_rollover_notification.assert_awaited_once()


@pytest.mark.asyncio
async def test_season_rollover_service_skips_invalid_payload() -> None:
    api = MagicMock()
    bot = AsyncMock()
    api.get_pending_season_rollover_notifications = AsyncMock(
        return_value=[
            {
                "eventId": "",
                "recipientTelegramIds": [0, -1],
            }
        ]
    )
    api.ack_season_rollover_notification = AsyncMock()

    service = SeasonRolloverNotificationsService(api=api, bot=bot, interval_seconds=60)
    await service._tick()

    bot.send_message.assert_not_awaited()
    api.ack_season_rollover_notification.assert_not_awaited()
