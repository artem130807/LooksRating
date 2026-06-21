from __future__ import annotations

import logging

from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from api.client import LooksRatingApiClient
from bot import texts
from bot.html_text import escape_telegram_html
from bot.keyboards import (
    BTN_RATING_EXIT,
    CALLBACK_RATING_MESSAGE_OK_PREFIX,
    CALLBACK_RATING_MESSAGE_REPLY_PREFIX,
    CALLBACK_RATING_MESSAGE_SHOW_PREFIX,
    rating_flow_keyboard,
    rating_message_reveal_keyboard,
)
from bot.states import RatingMessageStates, RatingStates
from handlers.rating import _resend_current_photo
from services.rating_user_message_service import RatingUserMessageService

router = Router()
logger = logging.getLogger(__name__)


@router.callback_query(RatingStates.awaiting_rating, F.data.startswith("msg:"))
async def rating_message_start(callback: CallbackQuery, state: FSMContext) -> None:
    photo_id = callback.data.split(":", 1)[1]
    data = await state.get_data()
    if photo_id != data.get("current_photo_id"):
        await callback.answer("Это фото уже неактуально", show_alert=True)
        return

    recipient_telegram_id = int(data.get("current_recipient_telegram_id") or 0)
    if recipient_telegram_id <= 0:
        await callback.answer(texts.RATING_MESSAGE_RECIPIENT_UNAVAILABLE, show_alert=True)
        return

    if callback.message is None:
        await callback.answer()
        return

    await state.set_state(RatingMessageStates.compose)
    await state.update_data(message_photo_id=photo_id)
    await callback.message.answer(texts.RATING_MESSAGE_PROMPT, reply_markup=rating_flow_keyboard())
    await callback.answer()


@router.message(RatingMessageStates.compose, F.text)
async def rating_message_compose_submit(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    rating_user_message_service: RatingUserMessageService,
) -> None:
    if message.text == BTN_RATING_EXIT:
        await state.set_state(RatingStates.awaiting_rating)
        await _resend_current_photo(message, state)
        return

    data = await state.get_data()
    photo_id = data.get("message_photo_id")
    if photo_id != data.get("current_photo_id"):
        await state.set_state(RatingStates.awaiting_rating)
        await message.answer("Это фото уже неактуально.", reply_markup=rating_flow_keyboard())
        return

    recipient_telegram_id = int(data.get("current_recipient_telegram_id") or 0)
    success, response_text = await rating_user_message_service.send_message(
        api,
        sender_telegram_id=message.from_user.id,
        sender_username=message.from_user.username,
        recipient_telegram_id=recipient_telegram_id,
        text=message.text or "",
    )
    await message.answer(response_text, reply_markup=rating_flow_keyboard())
    if not success:
        return

    await state.set_state(RatingStates.awaiting_rating)
    await _resend_current_photo(message, state)


@router.callback_query(F.data.startswith(CALLBACK_RATING_MESSAGE_SHOW_PREFIX))
async def rating_message_show(
    callback: CallbackQuery,
    rating_user_message_service: RatingUserMessageService,
) -> None:
    token = callback.data.removeprefix(CALLBACK_RATING_MESSAGE_SHOW_PREFIX)
    pending = await rating_user_message_service.get_pending_for_recipient(
        token,
        recipient_telegram_id=callback.from_user.id,
    )
    if pending is None:
        await callback.answer(texts.RATING_MESSAGE_NOT_FOUND, show_alert=True)
        return

    body = texts.RATING_MESSAGE_BODY.format(
        sender_name=escape_telegram_html(pending.sender_display_name),
        text=escape_telegram_html(pending.text),
    )
    if callback.message:
        await callback.message.edit_text(
            body,
            reply_markup=rating_message_reveal_keyboard(token),
        )
    await callback.answer()


@router.callback_query(F.data.startswith(CALLBACK_RATING_MESSAGE_REPLY_PREFIX))
async def rating_message_reply_start(
    callback: CallbackQuery,
    state: FSMContext,
    rating_user_message_service: RatingUserMessageService,
) -> None:
    token = callback.data.removeprefix(CALLBACK_RATING_MESSAGE_REPLY_PREFIX)
    pending = await rating_user_message_service.get_pending_for_recipient(
        token,
        recipient_telegram_id=callback.from_user.id,
    )
    if pending is None:
        await callback.answer(texts.RATING_MESSAGE_NOT_FOUND, show_alert=True)
        return

    await state.set_state(RatingMessageStates.reply_compose)
    await state.update_data(
        reply_recipient_telegram_id=pending.sender_telegram_id,
        reply_message_token=token,
    )
    if callback.message:
        await callback.message.answer(texts.RATING_MESSAGE_REPLY_PROMPT)
    await callback.answer()


@router.message(RatingMessageStates.reply_compose, F.text)
async def rating_message_reply_submit(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    rating_user_message_service: RatingUserMessageService,
) -> None:
    if message.text == BTN_RATING_EXIT:
        await state.clear()
        await message.answer(texts.RATING_MESSAGE_DISMISSED)
        return

    data = await state.get_data()
    recipient_telegram_id = int(data.get("reply_recipient_telegram_id") or 0)
    success, response_text = await rating_user_message_service.send_message(
        api,
        sender_telegram_id=message.from_user.id,
        sender_username=message.from_user.username,
        recipient_telegram_id=recipient_telegram_id,
        text=message.text or "",
    )
    await state.clear()
    await message.answer(response_text)

    token = data.get("reply_message_token")
    if success and token:
        await rating_user_message_service.dismiss(str(token))


@router.callback_query(F.data.startswith(CALLBACK_RATING_MESSAGE_OK_PREFIX))
async def rating_message_dismiss(
    callback: CallbackQuery,
    state: FSMContext,
    rating_user_message_service: RatingUserMessageService,
) -> None:
    token = callback.data.removeprefix(CALLBACK_RATING_MESSAGE_OK_PREFIX)
    pending = await rating_user_message_service.get_pending_for_recipient(
        token,
        recipient_telegram_id=callback.from_user.id,
    )
    if pending is None:
        await callback.answer(texts.RATING_MESSAGE_NOT_FOUND, show_alert=True)
        return

    await rating_user_message_service.dismiss(token)
    if callback.message:
        await callback.message.edit_text(texts.RATING_MESSAGE_DISMISSED)
    await state.clear()
    await callback.answer()
