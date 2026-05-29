import logging

from aiogram import F, Router
from aiogram.exceptions import TelegramBadRequest
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.keyboards import BTN_RATING_EXIT, rating_flow_keyboard, rating_keyboard
from bot.services import (
    SessionState,
    format_api_error,
    format_city_display,
    format_rating_display,
    send_main_menu,
    set_bot_state,
)
from bot.states import RatingStates, TicketStates

router = Router()
logger = logging.getLogger(__name__)


def _gender_icon(gender: str | None) -> str:
    normalized = (gender or "").strip().lower()
    if "муж" in normalized:
        return "👨"
    if "жен" in normalized:
        return "👩"
    if "оба" in normalized:
        return "👥"
    return "👤"


def _format_age_text(age: int | str | None) -> str:
    if isinstance(age, str):
        raw = age.strip()
        if raw.isdigit():
            age = int(raw)
        else:
            return f"{raw} лет"
    if not isinstance(age, int):
        return "— лет"
    n = abs(age)
    mod100 = n % 100
    mod10 = n % 10
    if 11 <= mod100 <= 14:
        suffix = "лет"
    elif mod10 == 1:
        suffix = "год"
    elif mod10 in {2, 3, 4}:
        suffix = "года"
    else:
        suffix = "лет"
    return f"{age} {suffix}"


def _normalize_photo_payload(photo: dict) -> dict:
    return {
        "id": photo.get("id") or photo.get("Id"),
        "telegramFileId": photo.get("telegramFileId") or photo.get("TelegramFileId"),
        "rank": photo.get("rank") or photo.get("Rank") or "—",
        "rating": photo.get("rating") if photo.get("rating") is not None else photo.get("Rating", 0),
        "ratingCount": photo.get("ratingCount") if photo.get("ratingCount") is not None else photo.get("RatingCount", 0),
        "displayName": photo.get("displayName") or photo.get("DisplayName"),
        "gender": photo.get("gender") or photo.get("Gender"),
        "age": photo.get("age") if photo.get("age") is not None else photo.get("Age"),
        "city": photo.get("city") or photo.get("City"),
    }


def _photo_caption(photo: dict) -> str:
    gender_text = photo.get("gender") or "Не указан"
    age_text = _format_age_text(photo.get("age"))
    caption = texts.RATING_CAPTION.format(
        display_name=photo.get("displayName") or "Участник",
        gender=gender_text,
        gender_icon=_gender_icon(gender_text),
        age_text=age_text,
        city=format_city_display(photo.get("city")),
        rank=photo.get("rank", "—"),
        rating_line=format_rating_display(
            float(photo.get("rating", 0)),
            int(photo.get("ratingCount", 0)),
        ),
    )
    return caption


async def exit_rating(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.IDLE)
    await send_main_menu(message, api, telegram_id, texts.RATING_EXIT)


async def show_next_photo(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    try:
        raw_photo = await api.get_next_photo(telegram_id)
    except ApiError as exc:
        await state.clear()
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await send_main_menu(message, api, telegram_id, format_api_error(exc))
        return

    photo = _normalize_photo_payload(raw_photo)
    photo_id = photo.get("id")
    file_id = photo.get("telegramFileId")
    if not photo_id or not file_id:
        await state.clear()
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await send_main_menu(
            message,
            api,
            telegram_id,
            "Не удалось получить фото для оценки. Попробуйте позже.",
        )
        return

    photo_id = str(photo_id)
    caption = _photo_caption(photo)
    await state.set_state(RatingStates.awaiting_rating)
    await state.update_data(
        current_photo_id=photo_id,
        current_file_id=file_id,
        current_caption=caption,
    )
    try:
        await message.answer_photo(
            file_id,
            caption=caption,
            reply_markup=rating_keyboard(photo_id),
        )
    except TelegramBadRequest as exc:
        logger.exception("Failed to send photo for rating: %s", exc)
        await state.clear()
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await send_main_menu(
            message,
            api,
            telegram_id,
            "Не удалось показать фото. Возможно, оно загружено другим ботом — добавьте новое фото в сезон.",
        )
        return

    await message.answer(texts.RATING_HINT, reply_markup=rating_flow_keyboard())


async def _resend_current_photo(message: Message, state: FSMContext) -> None:
    data = await state.get_data()
    file_id = data.get("current_file_id")
    caption = data.get("current_caption")
    photo_id = data.get("current_photo_id")
    if not file_id or not photo_id:
        return
    await state.set_state(RatingStates.awaiting_rating)
    await message.answer_photo(
        file_id,
        caption=caption,
        reply_markup=rating_keyboard(photo_id),
    )
    await message.answer(texts.RATING_HINT, reply_markup=rating_flow_keyboard())


@router.callback_query(RatingStates.awaiting_rating, F.data == "rate:exit")
async def rating_exit_callback(
    callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if callback.message:
        await exit_rating(callback.message, state, api, callback.from_user.id)
    await callback.answer()


@router.message(RatingStates.awaiting_rating, F.text == BTN_RATING_EXIT)
@router.message(TicketStates.description, F.text == BTN_RATING_EXIT)
async def rating_exit_message(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    await exit_rating(message, state, api, message.from_user.id)


@router.callback_query(RatingStates.awaiting_rating, F.data.startswith("rate:"))
async def rate_photo(
    callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if callback.data == "rate:exit":
        return

    _, photo_id, rating_str = callback.data.split(":", 2)
    data = await state.get_data()
    if photo_id != data.get("current_photo_id"):
        await callback.answer("Это фото уже неактуально", show_alert=True)
        return

    rating = int(rating_str)
    telegram_id = callback.from_user.id
    try:
        await api.create_review(telegram_id, photo_id, rating)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return

    await callback.answer(texts.RATING_SAVED.format(rating=rating))
    if callback.message:
        await show_next_photo(callback.message, state, api, telegram_id)


@router.callback_query(RatingStates.awaiting_rating, F.data.startswith("complain:"))
async def complain_start(callback: CallbackQuery, state: FSMContext) -> None:
    photo_id = callback.data.split(":", 1)[1]
    data = await state.get_data()
    if photo_id != data.get("current_photo_id"):
        await callback.answer("Это фото уже неактуально", show_alert=True)
        return
    await state.set_state(TicketStates.description)
    await state.update_data(complaint_photo_id=photo_id)
    await callback.message.answer(texts.COMPLAIN_PROMPT, reply_markup=rating_flow_keyboard())
    await callback.answer()


@router.message(TicketStates.description, F.text)
async def complain_submit(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text == BTN_RATING_EXIT:
        await exit_rating(message, state, api, message.from_user.id)
        return

    data = await state.get_data()
    photo_id = data.get("complaint_photo_id")
    if not photo_id:
        await state.clear()
        await set_bot_state(api, message.from_user.id, SessionState.IDLE)
        return
    description = message.text.strip()
    if len(description) < 3:
        await message.answer(
            "Опишите проблему подробнее (минимум 3 символа).",
            reply_markup=rating_flow_keyboard(),
        )
        return
    try:
        await api.create_ticket(message.from_user.id, photo_id, description)
    except ApiError as exc:
        await message.answer(format_api_error(exc), reply_markup=rating_flow_keyboard())
        return
    except ValueError:
        await message.answer(
            "Не удалось отправить жалобу: некорректное фото.",
            reply_markup=rating_flow_keyboard(),
        )
        await state.clear()
        await set_bot_state(api, message.from_user.id, SessionState.IDLE)
        await send_main_menu(message, api, message.from_user.id, texts.MAIN_MENU)
        return
    await message.answer(texts.COMPLAIN_DONE)
    await _resend_current_photo(message, state)


@router.message(RatingStates.awaiting_rating, F.text)
async def rating_requires_button(message: Message) -> None:
    if message.text == BTN_RATING_EXIT:
        return
    await message.answer(texts.RATING_REQUIRED_HINT, reply_markup=rating_flow_keyboard())
