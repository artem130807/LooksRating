from __future__ import annotations

from unittest.mock import MagicMock

import pytest

from bot import texts
from handlers.shop_gifts import shop_gift_confirm
from helpers.aiogram_builders import make_callback
from helpers.fakes import FakeApiClient
from services.writing_off_sparks_saga import (
    WritingOffSparksSagaResult,
    WritingOffSparksStep,
)


@pytest.mark.asyncio
async def test_shop_gift_confirm_uses_writing_off_saga_on_success() -> None:
    api = FakeApiClient(
        user={
            "hasVip": True,
            "sparksBalance": 5000,
        }
    )
    saga = MagicMock()
    saga.execute.return_value = WritingOffSparksSagaResult(
        success=True,
        message="saved",
        step=WritingOffSparksStep.COMPLETED,
    )
    callback = make_callback("shop:gift:confirm:100")
    callback.id = "callback-1"

    await shop_gift_confirm(callback, api, saga)

    saga.execute.assert_called_once_with(
        callback.from_user.id,
        100,
        idempotency_key="writing-off-sparks:42001:callback-1",
    )
    callback.message.edit_text.assert_any_call(texts.SHOP_GIFT_PROCESSING)
    final_text = callback.message.edit_text.call_args_list[-1].args[0]
    assert "в течение дня" in final_text.lower()
    assert "100" in final_text


@pytest.mark.asyncio
async def test_shop_gift_confirm_shows_failure_when_saga_fails() -> None:
    api = FakeApiClient(
        user={
            "hasVip": True,
            "sparksBalance": 5000,
        }
    )
    saga = MagicMock()
    saga.execute.return_value = WritingOffSparksSagaResult(
        success=False,
        message="Обмен не зафиксирован. Искры возвращены на баланс.",
        step=WritingOffSparksStep.COMPENSATION,
    )
    callback = make_callback("shop:gift:confirm:200")
    callback.id = "callback-2"

    await shop_gift_confirm(callback, api, saga)

    final_text = callback.message.edit_text.call_args_list[-1].args[0]
    assert "не удалось оформить обмен" in final_text.lower()
