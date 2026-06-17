from aiogram import F, Router
from aiogram.filters import StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, LabeledPrice, Message, PreCheckoutQuery
from aiogram.exceptions import TelegramBadRequest
import logging

from api.client import ApiError, LooksRatingApiClient
from bot import callbacks, texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import (
    MENU_ABOUT,
    MENU_PRIVILEGES,
    MENU_PROFILE,
    MENU_RATE,
    MENU_SETTINGS,
    MENU_TOP,
    rating_flow_keyboard,
)
from bot.states import FeedSetupStates, RatingStates, TicketStates
from bot.services import SessionState, format_api_error, main_menu_for, set_bot_state
from config import Settings
from handlers.feed_setup import begin_feed_setup
from handlers.rating import show_next_photo

router = Router()
VIP_PRODUCT_CODE = 1001
logger = logging.getLogger(__name__)
_MENU_BUTTONS = {
    MENU_RATE,
    MENU_ABOUT,
    MENU_TOP,
    MENU_PROFILE,
    MENU_SETTINGS,
    MENU_PRIVILEGES,
}


@router.message(
    StateFilter(
        RatingStates.awaiting_rating,
        TicketStates.description,
        FeedSetupStates.city,
        FeedSetupStates.age,
        FeedSetupStates.gender,
    ),
    F.text.in_(_MENU_BUTTONS),
)
async def menu_blocked_while_busy(message: Message) -> None:
    await message.answer(texts.MENU_BLOCKED_WHILE_BUSY)


async def start_rating_after_feed_setup(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.RATING)
    await message.answer(texts.RATING_START, reply_markup=rating_flow_keyboard())
    await show_next_photo(message, state, api, telegram_id)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_RATE)
async def menu_rate(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    telegram_id = message.from_user.id
    user = await api.get_user(telegram_id)
    if not user:
        await message.answer(texts.NEED_START)
        return

    if not user.get("hasRecommendationSettings"):
        await begin_feed_setup(message, state, api, telegram_id, start_rating_after=True)
        return

    await start_rating_after_feed_setup(message, state, api, telegram_id)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_ABOUT)
async def menu_about(message: Message, api: LooksRatingApiClient) -> None:
    await message.answer(texts.BOT_INFO)


@router.callback_query(F.data == callbacks.SHOP_VIP_BUY)
async def shop_buy_vip(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    settings = Settings.from_env()
    try:
        order = await api.create_payment_order(callback.from_user.id, VIP_PRODUCT_CODE)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return
    payload = order.get("payload")
    amount = int(order.get("amountStars", 0))
    if not payload or amount <= 0:
        await callback.answer(texts.SHOP_VIP_BUY_UNAVAILABLE, show_alert=True)
        return

    invoice_kwargs = {
        "title": order.get("productName", texts.SHOP_VIP_INVOICE_TITLE),
        "description": texts.SHOP_VIP_INVOICE_DESCRIPTION,
        "payload": payload,
        "currency": order.get("currency", "XTR"),
        "prices": [LabeledPrice(label="VIP", amount=amount)],
        "start_parameter": f"vip_{order.get('productCode', VIP_PRODUCT_CODE)}",
    }
    token = settings.stars_provider_token.strip()
    if token:
        invoice_kwargs["provider_token"] = token

    try:
        await callback.bot.send_invoice(
            chat_id=callback.from_user.id,
            **invoice_kwargs,
        )
    except TelegramBadRequest as exc:
        logger.warning("VIP invoice rejected for user %s: %s", callback.from_user.id, exc)
        await callback.answer(texts.SHOP_VIP_BUY_UNAVAILABLE, show_alert=True)
        return
    except Exception as exc:
        logger.exception("VIP invoice failed for user %s", callback.from_user.id, exc_info=exc)
        await callback.answer(texts.SHOP_VIP_BUY_UNAVAILABLE, show_alert=True)
        return

    await callback.answer()


@router.pre_checkout_query()
async def shop_pre_checkout(pre_checkout_query: PreCheckoutQuery) -> None:
    await pre_checkout_query.answer(ok=True)


@router.message(F.successful_payment)
async def shop_successful_payment(message: Message, api: LooksRatingApiClient) -> None:
    payment = message.successful_payment
    try:
        await api.confirm_payment_order(
            message.from_user.id,
            payload=payment.invoice_payload,
            telegram_payment_charge_id=payment.telegram_payment_charge_id,
            provider_payment_charge_id=payment.provider_payment_charge_id,
        )
    except Exception:
        await message.answer(texts.SHOP_VIP_PRECHECKOUT_FAILED)
        return
    await message.answer(texts.SHOP_VIP_PAID, reply_markup=await main_menu_for(api, message.from_user.id))
