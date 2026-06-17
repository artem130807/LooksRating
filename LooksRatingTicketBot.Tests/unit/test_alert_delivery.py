from __future__ import annotations

import asyncio
from unittest.mock import AsyncMock

import pytest
from aiogram.exceptions import TelegramForbiddenError

from bot.alert_delivery import AlertDeliveryService, format_alert_message


def test_format_alert_message_escapes_html():
    text = format_alert_message(
        {
            "severity": "critical",
            "title": "API <ready>",
            "body": "fail & down",
            "firstSeenAt": "2026-06-01T10:00:00Z",
        }
    )
    assert "API &lt;ready&gt;" in text
    assert "fail &amp; down" in text
    assert "🔴" in text


@pytest.mark.asyncio
async def test_alert_delivery_sends_and_acks():
    api = AsyncMock()
    api.monitoring_pending_alerts.return_value = {
        "alerts": [{"id": 7, "severity": "warning", "title": "Test", "body": "Body"}],
        "recipients": [111, 222],
    }
    bot = AsyncMock()
    service = AlertDeliveryService(api, bot, interval_seconds=60)

    await service._tick()

    assert bot.send_message.await_count == 2
    api.monitoring_ack_alert.assert_awaited_once_with(7)


@pytest.mark.asyncio
async def test_alert_delivery_skips_ack_on_partial_delivery():
    api = AsyncMock()
    api.monitoring_pending_alerts.return_value = {
        "alerts": [{"id": 7, "severity": "warning", "title": "Test", "body": "Body"}],
        "recipients": [111, 222],
    }
    bot = AsyncMock()
    bot.send_message.side_effect = [None, TelegramForbiddenError(method="sendMessage", message="blocked")]
    service = AlertDeliveryService(api, bot, interval_seconds=60)

    await service._tick()

    api.monitoring_ack_alert.assert_not_awaited()


@pytest.mark.asyncio
async def test_alert_delivery_skips_when_empty():
    api = AsyncMock()
    api.monitoring_pending_alerts.return_value = {"alerts": [], "recipients": [111]}
    bot = AsyncMock()
    service = AlertDeliveryService(api, bot, interval_seconds=60)

    await service._tick()

    bot.send_message.assert_not_awaited()
    api.monitoring_ack_alert.assert_not_awaited()
