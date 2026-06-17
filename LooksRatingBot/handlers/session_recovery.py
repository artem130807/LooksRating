from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from api.client import LooksRatingApiClient
from bot import texts
from bot.keyboards import BTN_RATING_EXIT, cancel_keyboard, rating_flow_keyboard
from bot.services import SessionState
from bot.session_sync import get_persisted_session_state, is_feed_setup_session, is_rating_session
from handlers.rating import exit_rating

router = Router()


@router.message(F.text == BTN_RATING_EXIT)
async def exit_rating_from_stale_ui(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    """Works after bot restart when FSM was lost but the rating keyboard is still visible."""
    await exit_rating(message, state, api, message.from_user.id)


@router.callback_query(F.data == "rate:exit")
async def exit_rating_callback_from_stale_ui(
    callback: CallbackQuery,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    if callback.message:
        await exit_rating(callback.message, state, api, callback.from_user.id)
    await callback.answer()


async def answer_orphan_session_hint(
    message: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> bool:
    api_state = await get_persisted_session_state(api, telegram_id)
    if is_rating_session(api_state):
        await message.answer(texts.RATING_REQUIRED_HINT, reply_markup=rating_flow_keyboard())
        return True
    if is_feed_setup_session(api_state):
        await message.answer(
            "Продолжите настройку ленты или нажмите «❌ Отмена».",
            reply_markup=cancel_keyboard(),
        )
        return True
    return False
