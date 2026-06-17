from __future__ import annotations

import asyncio
import logging

from aiogram import F, Router
from aiogram.types import CallbackQuery

from api.client import LooksRatingApiClient
from bot import callbacks, texts
from bot.keyboards import shop_gift_confirm_keyboard, shop_gifts_keyboard
from bot.services import (
    format_gift_failure_details,
    format_insufficient_sparks_alert,
    format_sparks_amount,
)
from handlers.privileges import show_vip_shop
from services.gift_purchase_saga import (
    ALLOWED_STAR_TIERS,
    GiftPurchaseSagaOrchestrator,
    GiftPurchaseStep,
    STAR_SPARKS_COSTS,
)

router = Router()
logger = logging.getLogger(__name__)


def _parse_stars_count(callback_data: str, prefix: str) -> int | None:
    raw = callback_data.removeprefix(prefix)
    try:
        stars = int(raw)
    except ValueError:
        return None
    return stars if stars in ALLOWED_STAR_TIERS else None


async def _require_vip_user(api: LooksRatingApiClient, telegram_id: int) -> dict | None:
    user = await api.get_user(telegram_id)
    if not user:
        return None
    if not user.get("hasVip"):
        return None
    return user


@router.callback_query(F.data == callbacks.SHOP_GIFTS)
async def shop_gifts_menu(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    user = await _require_vip_user(api, callback.from_user.id)
    if not user:
        await callback.answer(texts.SHOP_GIFTS_VIP_REQUIRED_ALERT, show_alert=True)
        return

    if callback.message:
        await callback.message.edit_text(
            texts.SHOP_GIFTS_MENU,
            reply_markup=shop_gifts_keyboard(),
        )
    await callback.answer()


@router.callback_query(F.data == callbacks.SHOP_BACK)
async def shop_back(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await show_vip_shop(callback.message, api, callback.from_user.id)
    await callback.answer()


@router.callback_query(F.data.startswith("shop:gift:select:"))
async def shop_gift_select(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    stars_count = _parse_stars_count(callback.data or "", "shop:gift:select:")
    if stars_count is None:
        await callback.answer("Некорректный подарок", show_alert=True)
        return

    user = await _require_vip_user(api, callback.from_user.id)
    if not user:
        await callback.answer(texts.SHOP_GIFTS_VIP_REQUIRED_ALERT, show_alert=True)
        return

    cost = STAR_SPARKS_COSTS[stars_count]
    balance_raw = user.get("sparksBalance", 0)
    try:
        balance = int(balance_raw)
    except (TypeError, ValueError):
        balance = 0

    if balance < cost:
        await callback.answer(
            format_insufficient_sparks_alert(cost, balance),
            show_alert=True,
        )
        return

    if callback.message:
        await callback.message.edit_text(
            texts.SHOP_GIFT_CONFIRM.format(
                stars=stars_count,
                cost=format_sparks_amount(cost),
                balance=format_sparks_amount(balance),
            ),
            reply_markup=shop_gift_confirm_keyboard(stars_count),
        )
    await callback.answer()


@router.callback_query(F.data.startswith("shop:gift:confirm:"))
async def shop_gift_confirm(
    callback: CallbackQuery,
    api: LooksRatingApiClient,
    gift_purchase_saga: GiftPurchaseSagaOrchestrator,
) -> None:
    stars_count = _parse_stars_count(callback.data or "", "shop:gift:confirm:")
    if stars_count is None:
        await callback.answer("Некорректный подарок", show_alert=True)
        return

    user = await _require_vip_user(api, callback.from_user.id)
    if not user:
        await callback.answer(texts.SHOP_GIFTS_VIP_REQUIRED_ALERT, show_alert=True)
        return

    cost = STAR_SPARKS_COSTS[stars_count]
    try:
        balance = int(user.get("sparksBalance", 0))
    except (TypeError, ValueError):
        balance = 0
    if balance < cost:
        await callback.answer(
            format_insufficient_sparks_alert(cost, balance),
            show_alert=True,
        )
        return

    await callback.answer()
    if callback.message:
        await callback.message.edit_text(texts.SHOP_GIFT_PROCESSING)

    try:
        result = await asyncio.to_thread(
            gift_purchase_saga.execute,
            callback.from_user.id,
            stars_count,
        )
    except Exception:
        logger.exception("Gift purchase saga failed for user %s", callback.from_user.id)
        if callback.message:
            await callback.message.edit_text(
                texts.SHOP_GIFT_FAILED.format(
                    details="Произошла ошибка. Попробуйте позже.",
                ),
                reply_markup=shop_gifts_keyboard(),
            )
        return

    if result.success:
        message_text = texts.SHOP_GIFT_SUCCESS.format(stars=stars_count)
    else:
        message_text = texts.SHOP_GIFT_FAILED.format(
            details=format_gift_failure_details(result.message),
        )

    if callback.message:
        await callback.message.edit_text(
            message_text,
            reply_markup=shop_gifts_keyboard(),
        )

    if result.step == GiftPurchaseStep.COMPENSATION and not result.success:
        logger.info(
            "Gift compensated for user %s stars=%s: %s",
            callback.from_user.id,
            stars_count,
            result.message,
        )
