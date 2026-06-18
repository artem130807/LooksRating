from __future__ import annotations

import logging
from dataclasses import dataclass
from enum import StrEnum

import grpc

from adapters.tgifts_grpc_client import TGiftsGrpcClient
from api.grpc_clients import LooksRatingSparksGrpcClient
from bot.sparks_exchange import ALLOWED_STAR_TIERS, sparks_cost, sparks_costs

logger = logging.getLogger(__name__)

STAR_SPARKS_COSTS: dict[int, int] = sparks_costs()


class GiftPurchaseStep(StrEnum):
    VALIDATION = "validation"
    DEBIT = "debit"
    GIFT_SEND = "gift_send"
    COMPENSATION = "compensation"
    COMPLETED = "completed"


@dataclass(frozen=True)
class GiftPurchaseSagaResult:
    success: bool
    message: str
    step: GiftPurchaseStep


class GiftPurchaseSagaOrchestrator:
    """Saga: debit sparks -> send Telegram gift -> compensate on gift failure."""

    def __init__(
        self,
        sparks_client: LooksRatingSparksGrpcClient,
        tgifts_client: TGiftsGrpcClient,
    ):
        self._sparks_client = sparks_client
        self._tgifts_client = tgifts_client

    def execute(self, telegram_id: int, stars_count: int) -> GiftPurchaseSagaResult:
        try:
            return self._execute(telegram_id, stars_count)
        except Exception:
            logger.exception(
                "Gift purchase saga crashed for telegram_id=%s stars=%s",
                telegram_id,
                stars_count,
            )
            return GiftPurchaseSagaResult(
                success=False,
                message="",
                step=GiftPurchaseStep.GIFT_SEND,
            )

    def _execute(self, telegram_id: int, stars_count: int) -> GiftPurchaseSagaResult:
        if stars_count not in ALLOWED_STAR_TIERS:
            return GiftPurchaseSagaResult(
                success=False,
                message="Недопустимая стоимость подарка",
                step=GiftPurchaseStep.VALIDATION,
            )

        debit = self._sparks_client.debited_sparks(telegram_id, stars_count)
        if not debit.success:
            return GiftPurchaseSagaResult(
                success=False,
                message=debit.message or "Не удалось списать искры",
                step=GiftPurchaseStep.DEBIT,
            )

        gift = self._tgifts_client.send_gift(telegram_id, stars_count)
        if gift.success:
            return GiftPurchaseSagaResult(
                success=True,
                message=gift.message or "Подарок успешно отправлен",
                step=GiftPurchaseStep.COMPLETED,
            )

        logger.warning(
            "Gift delivery failed for telegram_id=%s stars=%s: %s",
            telegram_id,
            stars_count,
            gift.message,
        )

        rollback = self._sparks_client.rollback_debited_sparks(
            telegram_id,
            stars_count,
            reason="gift_delivery_failed",
        )
        if not rollback.success:
            logger.error(
                "Compensation failed for telegram_id=%s stars=%s: %s",
                telegram_id,
                stars_count,
                rollback.message,
            )
            return GiftPurchaseSagaResult(
                success=False,
                message=(
                    "Подарок не отправлен, а откат искр не выполнен. "
                    "Обратитесь в поддержку."
                ),
                step=GiftPurchaseStep.COMPENSATION,
            )

        return GiftPurchaseSagaResult(
            success=False,
            message="Подарок не отправлен. Искры возвращены на баланс.",
            step=GiftPurchaseStep.COMPENSATION,
        )
