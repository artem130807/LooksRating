from __future__ import annotations

from unittest.mock import AsyncMock

import pytest
from pyrogram.errors.exceptions import RPCError

from helpers.fakes import FakePyrogramClient
from services.single_gift_dispatch import send_gift_to_user


@pytest.mark.asyncio
class TestSendGiftToUser:
    async def test_rejects_invalid_telegram_id(self) -> None:
        app = FakePyrogramClient()

        success, message = await send_gift_to_user(app, 0, 400)

        assert success is False
        assert "telegram_id" in message.lower()

    async def test_rejects_invalid_star_price(self) -> None:
        app = FakePyrogramClient()

        success, message = await send_gift_to_user(app, 1001, 0)

        assert success is False
        assert "цена" in message.lower()

    async def test_returns_error_when_gift_not_found(self) -> None:
        app = FakePyrogramClient(gifts=[{"id": 1, "price": 50}])

        success, message = await send_gift_to_user(app, 1001, 400)

        assert success is False
        assert "400" in message

    async def test_sends_gift_successfully(self, monkeypatch) -> None:
        app = FakePyrogramClient()
        buyer = AsyncMock()
        monkeypatch.setattr("services.single_gift_dispatch.buyer", buyer)
        monkeypatch.setattr("services.single_gift_dispatch.config.GIFT_DELAY", 0)

        success, message = await send_gift_to_user(app, 1001, 400)

        assert success is True
        assert "отправлен" in message.lower()
        buyer.assert_awaited_once_with(app, 1001, 101)

    async def test_maps_rpc_error(self, monkeypatch) -> None:
        app = FakePyrogramClient()
        app.get_users_error = RPCError("USER_BLOCKED")
        monkeypatch.setattr("services.single_gift_dispatch.config.GIFT_DELAY", 0)

        success, message = await send_gift_to_user(app, 1001, 400)

        assert success is False
        assert "USER_BLOCKED" in message
