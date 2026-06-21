from __future__ import annotations

import sys
from pathlib import Path

import pytest
from aiohttp import web
from aiohttp.test_utils import TestClient, TestServer

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "LooksRatingTicketBot"))

from api.main_bot_notify_client import MainBotNotifyClient  # noqa: E402


@pytest.mark.asyncio
async def test_notify_writing_off_sparks_confirmed_success() -> None:
    async def handler(request: web.Request) -> web.Response:
        assert request.headers["X-Internal-Notify-Key"] == "secret"
        payload = await request.json()
        assert payload["telegram_id"] == 42
        assert payload["stars"] == 100
        return web.json_response({"success": True, "message": "Notification delivered"})

    app = web.Application()
    app.router.add_post(
        "/internal/notifications/writing-off-sparks-confirmed",
        handler,
    )

    async with TestClient(TestServer(app)) as client:
        base_url = f"http://{client.server.host}:{client.server.port}"
        notify_client = MainBotNotifyClient(base_url, "secret")
        result = await notify_client.notify_writing_off_sparks_confirmed(
            telegram_id=42,
            stars=100,
        )

    assert result.success is True


@pytest.mark.asyncio
async def test_notify_writing_off_sparks_confirmed_unauthorized() -> None:
    async def handler(_: web.Request) -> web.Response:
        return web.json_response({"success": False, "message": "Unauthorized"}, status=401)

    app = web.Application()
    app.router.add_post(
        "/internal/notifications/writing-off-sparks-confirmed",
        handler,
    )

    async with TestClient(TestServer(app)) as client:
        base_url = f"http://{client.server.host}:{client.server.port}"
        notify_client = MainBotNotifyClient(base_url, "secret")
        result = await notify_client.notify_writing_off_sparks_confirmed(
            telegram_id=42,
            stars=100,
        )

    assert result.success is False
    assert "Unauthorized" in result.message


@pytest.mark.asyncio
async def test_notify_writing_off_sparks_cancelled_success() -> None:
    async def handler(request: web.Request) -> web.Response:
        assert request.headers["X-Internal-Notify-Key"] == "secret"
        payload = await request.json()
        assert payload["telegram_id"] == 42
        assert payload["stars"] == 100
        assert payload["sparks_count"] == 1200
        return web.json_response({"success": True, "message": "Notification delivered"})

    app = web.Application()
    app.router.add_post(
        "/internal/notifications/writing-off-sparks-cancelled",
        handler,
    )

    async with TestClient(TestServer(app)) as client:
        base_url = f"http://{client.server.host}:{client.server.port}"
        notify_client = MainBotNotifyClient(base_url, "secret")
        result = await notify_client.notify_writing_off_sparks_cancelled(
            telegram_id=42,
            stars=100,
            sparks_count=1200,
        )

    assert result.success is True
