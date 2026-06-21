"""Integration: TicketBot cancel handler → gRPC client (same contract as production)."""

from __future__ import annotations

import sys
from pathlib import Path
from unittest.mock import AsyncMock, MagicMock

import pytest
from unittest.mock import AsyncMock, MagicMock

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "LooksRatingTicketBot"))

from api.writing_off_sparks_client import UpdateStatusResult, WritingOffSparksDetail, WritingOffSparksItem  # noqa: E402
from bot import keyboards  # noqa: E402
from bot.withdrawal_views import OUTPUT_STATUS_CANCELLED  # noqa: E402
from handlers.withdrawals import on_withdrawal_cancel  # noqa: E402


@pytest.mark.asyncio
async def test_on_withdrawal_cancel_invokes_mark_cancelled_grpc() -> None:
    """TicketBot layer: admin cancel must call the same gRPC status as mark_cancelled."""
    request_id = "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
    grpc_client = MagicMock()
    grpc_client.get_by_id.return_value = WritingOffSparksDetail(
        success=True,
        message="ok",
        item=WritingOffSparksItem(
            id=request_id,
            status=1,
            user_id="user-1",
            telegram_id=42,
            city="moscow",
            sparks_count=1200,
            stars=100,
            created_at_unix_seconds=0,
        ),
    )
    grpc_client.mark_cancelled.return_value = UpdateStatusResult(
        success=True,
        message="Статус списания искр обновлён",
    )
    main_bot_notify = AsyncMock()
    main_bot_notify.notify_writing_off_sparks_cancelled.return_value = MagicMock(
        success=True,
        message="Notification delivered",
    )

    callback = MagicMock()
    callback.data = f"{keyboards.CALLBACK_PREFIX_WITHDRAWAL_CANCEL}{request_id}"
    callback.message = MagicMock()
    callback.message.answer = AsyncMock()
    callback.answer = AsyncMock()

    api = AsyncMock()
    state = AsyncMock()
    state.get_data = AsyncMock(return_value={})

    with pytest.MonkeyPatch.context() as monkeypatch:
        monkeypatch.setattr(
            "handlers.withdrawals.present_withdrawal_list",
            AsyncMock(),
        )
        monkeypatch.setattr(
            "handlers.withdrawals.require_authenticated_callback",
            AsyncMock(return_value=True),
        )
        await on_withdrawal_cancel(
            callback,
            state,
            api=api,
            writing_off_sparks=grpc_client,
            main_bot_notify=main_bot_notify,
        )

    grpc_client.get_by_id.assert_called_once_with(request_id)
    grpc_client.mark_cancelled.assert_called_once_with(request_id)
    main_bot_notify.notify_writing_off_sparks_cancelled.assert_awaited_once_with(
        telegram_id=42,
        stars=100,
        sparks_count=1200,
    )


def test_mark_cancelled_delegates_to_update_status_with_cancelled_enum() -> None:
    """WritingOffSparksGrpcClient must send OUTPUT_STATUS_CANCELLED (proto value 2)."""
    from api.writing_off_sparks_client import WritingOffSparksGrpcClient

    client = WritingOffSparksGrpcClient(address="localhost:0")
    captured: dict[str, object] = {}

    def fake_update_status(writing_off_sparks_id: str, status: int) -> UpdateStatusResult:
        captured["writing_off_sparks_id"] = writing_off_sparks_id
        captured["status"] = status
        return UpdateStatusResult(success=True, message="ok")

    client._update_status = fake_update_status  # type: ignore[method-assign]

    result = client.mark_cancelled("req-42")

    assert result.success is True
    assert captured == {
        "writing_off_sparks_id": "req-42",
        "status": OUTPUT_STATUS_CANCELLED,
    }
    assert OUTPUT_STATUS_CANCELLED == 2
