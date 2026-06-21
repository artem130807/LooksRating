from __future__ import annotations

from unittest.mock import MagicMock

import pytest

from adapters.writing_off_sparks_grpc_client import WritingOffSparksResponse
from api.grpc_clients import SparksGrpcResponse
from bot.sparks_exchange import ALLOWED_STAR_TIERS, sparks_cost
from services.writing_off_sparks_saga import (
    WritingOffSparksSagaOrchestrator,
    WritingOffSparksStep,
)

IDEMPOTENCY_KEY = "writing-off-sparks:10001:callback-1"


def _orchestrator(
    *,
    debit: SparksGrpcResponse | None = None,
    writing_off: WritingOffSparksResponse | None = None,
    rollback: SparksGrpcResponse | None = None,
) -> tuple[WritingOffSparksSagaOrchestrator, MagicMock, MagicMock]:
    sparks_client = MagicMock()
    sparks_client.debited_sparks.return_value = debit or SparksGrpcResponse(
        success=True,
        message="ok",
    )
    sparks_client.rollback_debited_sparks.return_value = rollback or SparksGrpcResponse(
        success=True,
        message="rolled back",
    )

    writing_off_client = MagicMock()
    writing_off_client.create_writing_off_sparks.return_value = writing_off or WritingOffSparksResponse(
        success=True,
        message="saved",
    )

    return (
        WritingOffSparksSagaOrchestrator(sparks_client, writing_off_client),
        sparks_client,
        writing_off_client,
    )


def test_execute_completes_when_debit_and_writing_off_succeed() -> None:
    orchestrator, sparks_client, writing_off_client = _orchestrator(
        writing_off=WritingOffSparksResponse(success=True, message="saved"),
    )

    result = orchestrator.execute(
        telegram_id=10001,
        stars_count=100,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is True
    assert result.message == "saved"
    assert result.step == WritingOffSparksStep.COMPLETED
    sparks_client.debited_sparks.assert_called_once_with(
        10001,
        100,
        idempotency_key=IDEMPOTENCY_KEY,
    )
    writing_off_client.create_writing_off_sparks.assert_called_once_with(
        10001,
        1200,
        100,
        idempotency_key=IDEMPOTENCY_KEY,
    )
    sparks_client.rollback_debited_sparks.assert_not_called()


def test_execute_uses_default_success_message_when_writing_off_message_empty() -> None:
    orchestrator, _, _ = _orchestrator(
        writing_off=WritingOffSparksResponse(success=True, message=""),
    )

    result = orchestrator.execute(
        telegram_id=10001,
        stars_count=100,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is True
    assert result.message == "Обмен успешно зафиксирован"


@pytest.mark.parametrize("stars_count", sorted(ALLOWED_STAR_TIERS))
def test_execute_passes_sparks_cost_for_each_allowed_tier(stars_count: int) -> None:
    orchestrator, sparks_client, writing_off_client = _orchestrator()
    expected_sparks = sparks_cost(stars_count)
    assert expected_sparks is not None

    orchestrator.execute(
        telegram_id=42,
        stars_count=stars_count,
        idempotency_key=f"writing-off-sparks:42:tier-{stars_count}",
    )

    sparks_client.debited_sparks.assert_called_once_with(
        42,
        stars_count,
        idempotency_key=f"writing-off-sparks:42:tier-{stars_count}",
    )
    writing_off_client.create_writing_off_sparks.assert_called_once_with(
        42,
        expected_sparks,
        stars_count,
        idempotency_key=f"writing-off-sparks:42:tier-{stars_count}",
    )


@pytest.mark.parametrize("stars_count", [0, 50, 150, 500])
def test_execute_rejects_invalid_star_tier(stars_count: int) -> None:
    sparks_client = MagicMock()
    writing_off_client = MagicMock()
    orchestrator = WritingOffSparksSagaOrchestrator(sparks_client, writing_off_client)

    result = orchestrator.execute(
        telegram_id=10003,
        stars_count=stars_count,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is False
    assert result.message == "Недопустимая стоимость подарка"
    assert result.step == WritingOffSparksStep.VALIDATION
    sparks_client.debited_sparks.assert_not_called()
    writing_off_client.create_writing_off_sparks.assert_not_called()


def test_execute_rejects_missing_idempotency_key() -> None:
    orchestrator, sparks_client, writing_off_client = _orchestrator()

    result = orchestrator.execute(telegram_id=10003, stars_count=100, idempotency_key="  ")

    assert result.success is False
    assert result.message == "Ключ идемпотентности не указан"
    assert result.step == WritingOffSparksStep.VALIDATION
    sparks_client.debited_sparks.assert_not_called()
    writing_off_client.create_writing_off_sparks.assert_not_called()


def test_execute_returns_debit_step_when_debit_fails() -> None:
    orchestrator, sparks_client, writing_off_client = _orchestrator(
        debit=SparksGrpcResponse(success=False, message="Недостаточно искр"),
    )

    result = orchestrator.execute(
        telegram_id=10004,
        stars_count=200,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is False
    assert result.message == "Недостаточно искр"
    assert result.step == WritingOffSparksStep.DEBIT
    sparks_client.debited_sparks.assert_called_once_with(10004, 200, idempotency_key=IDEMPOTENCY_KEY)
    writing_off_client.create_writing_off_sparks.assert_not_called()
    sparks_client.rollback_debited_sparks.assert_not_called()


def test_execute_uses_default_debit_message_when_empty() -> None:
    orchestrator, _, _ = _orchestrator(
        debit=SparksGrpcResponse(success=False, message=""),
    )

    result = orchestrator.execute(
        telegram_id=10005,
        stars_count=300,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is False
    assert result.message == "Не удалось списать искры"
    assert result.step == WritingOffSparksStep.DEBIT


def test_execute_rolls_back_when_writing_off_fails() -> None:
    orchestrator, sparks_client, writing_off_client = _orchestrator(
        writing_off=WritingOffSparksResponse(success=False, message="db error"),
    )

    result = orchestrator.execute(
        telegram_id=10002,
        stars_count=200,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is False
    assert result.message == "Обмен не зафиксирован. Искры возвращены на баланс."
    assert result.step == WritingOffSparksStep.COMPENSATION
    assert writing_off_client.create_writing_off_sparks.call_count == 2
    writing_off_client.create_writing_off_sparks.assert_called_with(
        10002,
        2400,
        200,
        idempotency_key=IDEMPOTENCY_KEY,
    )
    sparks_client.rollback_debited_sparks.assert_called_once_with(
        10002,
        200,
        reason="writing_off_failed",
        idempotency_key=IDEMPOTENCY_KEY,
    )


def test_execute_recovers_when_idempotent_retry_succeeds_after_create_failure() -> None:
    orchestrator, sparks_client, writing_off_client = _orchestrator(
        writing_off=WritingOffSparksResponse(success=False, message="timeout"),
    )
    writing_off_client.create_writing_off_sparks.side_effect = [
        WritingOffSparksResponse(success=False, message="timeout"),
        WritingOffSparksResponse(success=True, message="Заявка уже создана"),
    ]

    result = orchestrator.execute(
        telegram_id=10008,
        stars_count=200,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is True
    assert result.message == "Заявка уже создана"
    assert result.step == WritingOffSparksStep.COMPLETED
    assert writing_off_client.create_writing_off_sparks.call_count == 2
    sparks_client.rollback_debited_sparks.assert_not_called()


def test_execute_returns_compensation_when_rollback_fails() -> None:
    orchestrator, sparks_client, _ = _orchestrator(
        writing_off=WritingOffSparksResponse(success=False, message="db error"),
        rollback=SparksGrpcResponse(success=False, message="rollback failed"),
    )

    result = orchestrator.execute(
        telegram_id=10006,
        stars_count=400,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is False
    assert result.message == (
        "Обмен не зафиксирован, а откат искр не выполнен. "
        "Обратитесь в поддержку."
    )
    assert result.step == WritingOffSparksStep.COMPENSATION
    sparks_client.rollback_debited_sparks.assert_called_once_with(
        10006,
        400,
        reason="writing_off_failed",
        idempotency_key=IDEMPOTENCY_KEY,
    )


def test_execute_crashed_returns_writing_off_step_on_unexpected_exception() -> None:
    sparks_client = MagicMock()
    sparks_client.debited_sparks.side_effect = RuntimeError("network down")
    orchestrator = WritingOffSparksSagaOrchestrator(sparks_client, MagicMock())

    result = orchestrator.execute(
        telegram_id=10007,
        stars_count=100,
        idempotency_key=IDEMPOTENCY_KEY,
    )

    assert result.success is False
    assert result.message == ""
    assert result.step == WritingOffSparksStep.WRITING_OFF
