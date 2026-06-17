from __future__ import annotations

from unittest.mock import AsyncMock

import pytest

from grpc_gen import send_gift_pb2
from grpc_server.gift_send_service import TelegramGiftSenderServicer


@pytest.mark.asyncio
class TestTelegramGiftSenderServicer:
    async def test_send_gift_returns_success_response(self, monkeypatch) -> None:
        async def fake_send(_app, telegram_id: int, star_price: int) -> tuple[bool, str]:
            assert telegram_id == 42_001
            assert star_price == 400
            return True, "Подарок отправлен"

        monkeypatch.setattr("grpc_server.gift_send_service.send_gift_to_user", fake_send)
        servicer = TelegramGiftSenderServicer(app=object())
        request = send_gift_pb2.SendGiftRequest(recipient_telegram_id=42_001, star_price=400)

        response = await servicer.SendGift(request, None)

        assert response.success is True
        assert "отправлен" in response.message.lower()

    async def test_send_gift_returns_failure_response(self, monkeypatch) -> None:
        monkeypatch.setattr(
            "grpc_server.gift_send_service.send_gift_to_user",
            AsyncMock(return_value=(False, "Подарок за 400★ не найден")),
        )
        servicer = TelegramGiftSenderServicer(app=object())
        request = send_gift_pb2.SendGiftRequest(recipient_telegram_id=1, star_price=400)

        response = await servicer.SendGift(request, None)

        assert response.success is False
        assert "400" in response.message
