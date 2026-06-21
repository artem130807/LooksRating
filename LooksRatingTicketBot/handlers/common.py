from __future__ import annotations

import logging

from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, TicketApiClient
from bot import keyboards, texts
from bot.session_sync import (
    API_STATE_AWAITING_LOGIN,
    API_STATE_AWAITING_PASSWORD,
    is_authenticated,
    restore_fsm_from_api,
    session_payload,
    session_state,
)
from bot.states import AuthStates

logger = logging.getLogger(__name__)


async def load_session(
    api: TicketApiClient,
    telegram_id: int,
    *,
    create: bool = True,
) -> dict | None:
    session = await api.get_session(telegram_id)
    if session is not None:
        return session
    if not create:
        return None
    return await api.ensure_session(telegram_id)


async def show_guest_home(message: Message) -> None:
    await message.answer(texts.START_UNAUTH, reply_markup=keyboards.start_unauthenticated())


async def reset_auth_flow(
    api: TicketApiClient,
    state: FSMContext,
    telegram_id: int,
    *,
    message: Message | None = None,
    notice: str | None = None,
    ask_login: bool = True,
) -> None:
    try:
        await api.begin_login(telegram_id)
    except ApiError as exc:
        logger.warning("begin_login failed during auth reset for %s: %s", telegram_id, exc.message)

    await state.clear()
    await state.set_state(AuthStates.awaiting_login)

    if message is None:
        return
    if notice:
        await message.answer(notice)
    if ask_login:
        await message.answer(texts.ASK_LOGIN)


async def route_guest_message(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
) -> bool:
    """Handle unauthenticated text. Returns True if handled."""
    telegram_id = message.from_user.id
    session = await load_session(api, telegram_id)
    await restore_fsm_from_api(state, api, telegram_id, session)

    if session and is_authenticated(session):
        return False

    api_state = session_state(session)
    logger.info("guest message from %s, api_state=%s", telegram_id, api_state or "start")

    if api_state == API_STATE_AWAITING_LOGIN:
        await process_login_text(message, state, api)
        return True

    if api_state == API_STATE_AWAITING_PASSWORD:
        await process_password_text(message, state, api)
        return True

    if api_state in {"start", "", "idle"}:
        await show_guest_home(message)
        return True

    return False


async def process_login_text(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
) -> None:
    from bot import texts

    login = (message.text or "").strip()
    if not login:
        await message.answer(texts.ASK_LOGIN)
        return

    telegram_id = message.from_user.id
    try:
        await api.submit_login(telegram_id, login)
    except ApiError as exc:
        if exc.status != 404:
            raise
        logger.warning("submit-login unavailable, using local auth state only")
    await state.update_data(login=login)
    await state.set_state(AuthStates.awaiting_password)
    await message.answer(texts.ASK_PASSWORD)


async def process_password_text(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
) -> None:
    from bot import texts
    from handlers.withdrawals import show_moderation_hub

    password = (message.text or "").strip()
    if not password:
        await message.answer(texts.ASK_PASSWORD)
        return

    data = await state.get_data()
    login = str(data.get("login", "")).strip()
    if not login:
        payload = session_payload(await load_session(api, message.from_user.id))
        login = payload.get("login", "").strip()
    if not login:
        await state.set_state(AuthStates.awaiting_login)
        await message.answer(texts.ASK_LOGIN)
        return

    telegram_id = message.from_user.id
    try:
        await api.authenticate(telegram_id, login, password)
    except ApiError as exc:
        if exc.status == 429:
            await message.answer("Слишком много попыток входа. Подождите минуту и попробуйте снова.")
            return
        if exc.status in {400, 401}:
            await reset_auth_flow(
                api,
                state,
                telegram_id,
                message=message,
                notice=texts.INVALID_CREDENTIALS,
            )
            return
        raise

    await state.clear()
    await message.answer(texts.LOGIN_SUCCESS, reply_markup=keyboards.admin_reply_keyboard())
    await show_moderation_hub(message, state)
