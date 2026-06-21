import logging

from aiogram import F, Router
from aiogram.filters import CommandStart
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message, ReplyKeyboardRemove

from api.client import ApiError, TicketApiClient
from bot import keyboards, texts
from bot.session_sync import (
    API_STATE_MODERATING,
    is_authenticated,
    restore_fsm_from_api,
    session_state,
)
from bot.states import AuthStates
from bot.telegram_media import MainBotMediaService
from handlers.common import reset_auth_flow
from handlers.moderation import present_current_ticket
from handlers.withdrawals import show_moderation_hub

router = Router()
logger = logging.getLogger(__name__)


async def resume_authenticated_flow(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
    telegram_id: int,
    session: dict,
) -> bool:
    api_state = session_state(session)
    if api_state == API_STATE_MODERATING:
        await message.answer(
            "Продолжаем модерацию с того места, где остановились.",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        await present_current_ticket(
            message, api, telegram_id, main_bot_media=main_bot_media, state=state
        )
        return True

    await message.answer(texts.START_AUTH, reply_markup=keyboards.admin_reply_keyboard())
    if api_state in {"awaiting_city", "authenticated"}:
        await show_moderation_hub(message, state)
    return True


@router.message(CommandStart())
@router.message(F.text.startswith("/start"))
async def on_start(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    telegram_id = message.from_user.id
    try:
        session = await api.ensure_session(telegram_id)
    except ApiError as exc:
        logger.error("ensure_session failed for %s: %s", telegram_id, exc.message)
        await message.answer(f"Ошибка API: {exc.message}")
        return

    logger.info("start from %s, authenticated=%s, state=%s", telegram_id, session.get("isAuthenticated"), session.get("state"))

    if is_authenticated(session):
        await restore_fsm_from_api(state, api, telegram_id, session)
        await resume_authenticated_flow(
            message, state, api, main_bot_media, telegram_id, session
        )
        return

    await reset_auth_flow(api, state, telegram_id, message=None)
    await message.answer(texts.START_UNAUTH, reply_markup=keyboards.start_unauthenticated())


@router.callback_query(F.data == keyboards.CALLBACK_LOGIN)
async def on_login_click(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    await callback.answer()
    try:
        await api.begin_login(callback.from_user.id)
    except ApiError as exc:
        await callback.message.answer(f"Ошибка API: {exc.message}")
        return
    await state.set_state(AuthStates.awaiting_login)
    await callback.message.answer(texts.ASK_LOGIN)


@router.callback_query(F.data == keyboards.CALLBACK_LOGOUT)
async def on_logout(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    await callback.answer()
    await state.clear()
    try:
        await api.logout(callback.from_user.id)
    except ApiError as exc:
        logger.warning("logout failed for %s: %s", callback.from_user.id, exc.message)
    if callback.message:
        await callback.message.answer(texts.LOGOUT_OK, reply_markup=ReplyKeyboardRemove())
        await callback.message.answer(texts.START_UNAUTH, reply_markup=keyboards.start_unauthenticated())
