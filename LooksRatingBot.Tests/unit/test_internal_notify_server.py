from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock

import pytest
from aiohttp import web
from aiohttp.test_utils import TestClient, TestServer

from bot.internal_notify_server import create_internal_notify_app
from bot.writing_off_sparks_user_notifier import WritingOffSparksUserNotifier


@pytest.mark.asyncio
async def test_internal_notify_requires_api_key() -> None:
    notifier = MagicMock(spec=WritingOffSparksUserNotifier)
    app = create_internal_notify_app(notifier, api_key="secret")
    async with TestClient(TestServer(app)) as client:
        response = await client.post(
            "/internal/notifications/writing-off-sparks-confirmed",
            json={"telegram_id": 1, "stars": 100},
        )

    assert response.status == 401


@pytest.mark.asyncio
async def test_internal_notify_delivers_confirmed_with_valid_key() -> None:
    notifier = MagicMock(spec=WritingOffSparksUserNotifier)
    notifier.notify_confirmed = AsyncMock(return_value=True)

    app = create_internal_notify_app(notifier, api_key="secret")
    async with TestClient(TestServer(app)) as client:
        response = await client.post(
            "/internal/notifications/writing-off-sparks-confirmed",
            headers={"X-Internal-Notify-Key": "secret"},
            json={"telegram_id": 42, "stars": 100},
        )
        assert response.status == 200
        payload = await response.json()
        assert payload["success"] is True

    notifier.notify_confirmed.assert_awaited_once_with(telegram_id=42, stars=100)


@pytest.mark.asyncio
async def test_internal_notify_delivers_cancelled_with_valid_key() -> None:
    notifier = MagicMock(spec=WritingOffSparksUserNotifier)
    notifier.notify_cancelled = AsyncMock(return_value=True)

    app = create_internal_notify_app(notifier, api_key="secret")
    async with TestClient(TestServer(app)) as client:
        response = await client.post(
            "/internal/notifications/writing-off-sparks-cancelled",
            headers={"X-Internal-Notify-Key": "secret"},
            json={"telegram_id": 42, "stars": 100, "sparks_count": 1200},
        )
        assert response.status == 200
        payload = await response.json()
        assert payload["success"] is True

    notifier.notify_cancelled.assert_awaited_once_with(
        telegram_id=42,
        stars=100,
        sparks=1200,
    )
