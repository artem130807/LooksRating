from __future__ import annotations

import logging
from dataclasses import dataclass
from enum import StrEnum

from adapters.writing_off_sparks_grpc_client import WritingOffSparksGrpcClient
from api.grpc_clients import LooksRatingSparksGrpcClient
from bot.sparks_exchange import ALLOWED_STAR_TIERS, sparks_cost

logger = logging.getLogger(__name__)


class WritingOffSparksStep(StrEnum):
    VALIDATION = "validation"
    DEBIT = "debit"
    WRITING_OFF = "writing_off"
    COMPENSATION = "compensation"
    COMPLETED = "completed"


@dataclass(frozen=True)
class WritingOffSparksSagaResult:
    success: bool
    message: str
    step: WritingOffSparksStep


class WritingOffSparksSagaOrchestrator:
    """Saga: debit sparks -> persist writing-off via API -> compensate on failure."""

    def __init__(
        self,
        sparks_client: LooksRatingSparksGrpcClient,
        writing_off_client: WritingOffSparksGrpcClient,
    ):
        self._sparks_client = sparks_client
        self._writing_off_client = writing_off_client

    def execute(
        self,
        telegram_id: int,
        stars_count: int,
        *,
        idempotency_key: str,
    ) -> WritingOffSparksSagaResult:
        try:
            return self._execute(telegram_id, stars_count, idempotency_key=idempotency_key)
        except Exception:
            logger.exception(
                "Writing-off sparks saga crashed for telegram_id=%s stars=%s",
                telegram_id,
                stars_count,
            )
            return WritingOffSparksSagaResult(
                success=False,
                message="",
                step=WritingOffSparksStep.WRITING_OFF,
            )

    def _execute(
        self,
        telegram_id: int,
        stars_count: int,
        *,
        idempotency_key: str,
    ) -> WritingOffSparksSagaResult:
        normalized_key = (idempotency_key or "").strip()
        if not normalized_key:
            return WritingOffSparksSagaResult(
                success=False,
                message="Ключ идемпотентности не указан",
                step=WritingOffSparksStep.VALIDATION,
            )

        if stars_count not in ALLOWED_STAR_TIERS:
            return WritingOffSparksSagaResult(
                success=False,
                message="Недопустимая стоимость подарка",
                step=WritingOffSparksStep.VALIDATION,
            )

        sparks_count = sparks_cost(stars_count)
        if sparks_count is None:
            return WritingOffSparksSagaResult(
                success=False,
                message="Недопустимая стоимость подарка",
                step=WritingOffSparksStep.VALIDATION,
            )

        debit = self._sparks_client.debited_sparks(
            telegram_id,
            stars_count,
            idempotency_key=normalized_key,
        )
        if not debit.success:
            return WritingOffSparksSagaResult(
                success=False,
                message=debit.message or "Не удалось списать искры",
                step=WritingOffSparksStep.DEBIT,
            )

        writing_off = self._writing_off_client.create_writing_off_sparks(
            telegram_id,
            sparks_count,
            stars_count,
            idempotency_key=normalized_key,
        )
        if writing_off.success:
            return WritingOffSparksSagaResult(
                success=True,
                message=writing_off.message or "Обмен успешно зафиксирован",
                step=WritingOffSparksStep.COMPLETED,
            )

        recovery = self._writing_off_client.create_writing_off_sparks(
            telegram_id,
            sparks_count,
            stars_count,
            idempotency_key=normalized_key,
        )
        if recovery.success:
            logger.info(
                "Writing-off recovered via idempotent retry for telegram_id=%s stars=%s",
                telegram_id,
                stars_count,
            )
            return WritingOffSparksSagaResult(
                success=True,
                message=recovery.message or "Обмен успешно зафиксирован",
                step=WritingOffSparksStep.COMPLETED,
            )

        logger.warning(
            "Writing-off failed for telegram_id=%s stars=%s sparks=%s: %s",
            telegram_id,
            stars_count,
            sparks_count,
            writing_off.message,
        )

        rollback = self._sparks_client.rollback_debited_sparks(
            telegram_id,
            stars_count,
            reason="writing_off_failed",
            idempotency_key=normalized_key,
        )
        if not rollback.success:
            logger.error(
                "Compensation failed for telegram_id=%s stars=%s: %s",
                telegram_id,
                stars_count,
                rollback.message,
            )
            return WritingOffSparksSagaResult(
                success=False,
                message=(
                    "Обмен не зафиксирован, а откат искр не выполнен. "
                    "Обратитесь в поддержку."
                ),
                step=WritingOffSparksStep.COMPENSATION,
            )

        return WritingOffSparksSagaResult(
            success=False,
            message="Обмен не зафиксирован. Искры возвращены на баланс.",
            step=WritingOffSparksStep.COMPENSATION,
        )
