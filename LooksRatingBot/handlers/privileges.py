from __future__ import annotations

import logging

from aiogram import F, Router
from aiogram.types import CallbackQuery, Message

from api.client import ApiError, LooksRatingApiClient
from bot import callbacks, texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import (
    MENU_PRIVILEGES,
    privileges_hub_keyboard,
    referral_program_keyboard,
    vip_shop_keyboard,
)
from bot.referral_presenter import format_referral_program_message
from bot.services import send_main_menu
from bot.telegram_edit import edit_text_or_ignore_unchanged
from services.referral_program import ReferralLinkService
from services.referral_share import build_telegram_share_url

router = Router()
logger = logging.getLogger(__name__)


async def _user_has_vip(api: LooksRatingApiClient, telegram_id: int) -> bool:
    user = await api.get_user(telegram_id)
    return bool(user and user.get("hasVip"))


async def show_privileges_hub(
    message: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
    *,
    edit: bool = False,
) -> None:
    has_vip = await _user_has_vip(api, telegram_id)
    markup = privileges_hub_keyboard(has_vip=has_vip)
    if edit:
        await edit_text_or_ignore_unchanged(message, texts.PRIVILEGES_HUB, reply_markup=markup)
    else:
        await message.answer(texts.PRIVILEGES_HUB, reply_markup=markup)


async def show_vip_shop(
    message: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    has_vip = await _user_has_vip(api, telegram_id)
    await edit_text_or_ignore_unchanged(
        message,
        texts.VIP_SHOP_MENU,
        reply_markup=vip_shop_keyboard(has_vip=has_vip),
    )


async def show_referral_program(
    message: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    service = ReferralLinkService(api)
    try:
        view = await service.get_or_create_link(telegram_id)
    except ApiError as exc:
        logger.warning("Referral link failed for %s: %s", telegram_id, exc)
        await edit_text_or_ignore_unchanged(
            message,
            texts.REFERRAL_PROGRAM_UNAVAILABLE,
            reply_markup=referral_program_keyboard(share_url=None),
        )
        return

    await edit_text_or_ignore_unchanged(
        message,
        format_referral_program_message(view),
        reply_markup=referral_program_keyboard(
            share_url=build_telegram_share_url(view.link),
        ),
    )


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_PRIVILEGES)
async def menu_privileges(message: Message, api: LooksRatingApiClient) -> None:
    user = await api.get_user(message.from_user.id)
    if not user:
        await message.answer(texts.NEED_START)
        return
    await show_privileges_hub(message, api, message.from_user.id, edit=False)


@router.callback_query(F.data == callbacks.PRIVILEGES_HUB)
async def privileges_hub_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await show_privileges_hub(callback.message, api, callback.from_user.id, edit=True)
    await callback.answer()


@router.callback_query(F.data == callbacks.PRIVILEGES_VIP)
async def privileges_vip_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await show_vip_shop(callback.message, api, callback.from_user.id)
    await callback.answer()


@router.callback_query(F.data == callbacks.PRIVILEGES_REFERRAL)
async def privileges_referral_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if not callback.message:
        await callback.answer()
        return

    await callback.answer(texts.REFERRAL_PROGRAM_LOADING, show_alert=False)
    await show_referral_program(callback.message, api, callback.from_user.id)


@router.callback_query(F.data == callbacks.SHOP_MAIN_MENU)
async def privileges_main_menu(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await send_main_menu(callback.message, api, callback.from_user.id, texts.MAIN_MENU)
    await callback.answer()
