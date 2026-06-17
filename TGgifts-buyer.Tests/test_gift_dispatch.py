from __future__ import annotations

from unittest.mock import AsyncMock

import pytest
from pyrogram.errors.exceptions import RPCError

from helpers.fakes import FakePyrogramClient
from services.gift_dispatch import dispatch_ranked_gifts
from services.vip_top_models import VipGiftRecipient


@pytest.mark.asyncio
class TestDispatchRankedGifts:
    async def test_empty_recipients_returns_zero_counts(self) -> None:
        app = FakePyrogramClient()

        success, failed = await dispatch_ranked_gifts(app, [], {400: 101})

        assert success == 0
        assert failed == 0

    async def test_dispatches_gifts_for_recipients(self, monkeypatch) -> None:
        app = FakePyrogramClient()
        buyer = AsyncMock()
        monkeypatch.setattr("services.gift_dispatch.buyer", buyer)
        monkeypatch.setattr("services.gift_dispatch.config.GIFT_DELAY", 0)

        recipients = [
            VipGiftRecipient(telegram_id=1001, place=1, star_price=400),
            VipGiftRecipient(telegram_id=1002, place=2, star_price=300),
        ]
        gift_ids = {400: 101, 300: 102}

        success, failed = await dispatch_ranked_gifts(app, recipients, gift_ids)

        assert success == 2
        assert failed == 0
        assert buyer.await_count == 2
        buyer.assert_any_await(app, 1001, 101)
        buyer.assert_any_await(app, 1002, 102)

    async def test_missing_gift_price_counts_as_failed(self, monkeypatch) -> None:
        app = FakePyrogramClient()
        buyer = AsyncMock()
        monkeypatch.setattr("services.gift_dispatch.buyer", buyer)

        recipients = [VipGiftRecipient(telegram_id=1001, place=1, star_price=999)]

        success, failed = await dispatch_ranked_gifts(app, recipients, {400: 101})

        assert success == 0
        assert failed == 1
        buyer.assert_not_awaited()

    async def test_rpc_error_counts_as_failed(self, monkeypatch) -> None:
        app = FakePyrogramClient()
        app.get_users_error = RPCError("boom")
        buyer = AsyncMock()
        monkeypatch.setattr("services.gift_dispatch.buyer", buyer)
        monkeypatch.setattr("services.gift_dispatch.config.GIFT_DELAY", 0)

        recipients = [VipGiftRecipient(telegram_id=1001, place=1, star_price=400)]

        success, failed = await dispatch_ranked_gifts(app, recipients, {400: 101})

        assert success == 0
        assert failed == 1

    async def test_send_intro_message_when_enabled(self, monkeypatch) -> None:
        app = FakePyrogramClient()
        buyer = AsyncMock()
        monkeypatch.setattr("services.gift_dispatch.buyer", buyer)
        monkeypatch.setattr("services.gift_dispatch.config.GIFT_DELAY", 0)

        recipients = [VipGiftRecipient(telegram_id=1001, place=1, star_price=400)]

        await dispatch_ranked_gifts(
            app,
            recipients,
            {400: 101},
            send_intro_message=True,
        )

        assert len(app.sent_messages) == 1
        assert app.sent_messages[0][0] == 1001
        assert "LooksRating" in app.sent_messages[0][1]
