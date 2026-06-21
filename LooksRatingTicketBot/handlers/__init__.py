import logging

from aiogram import F, Router
from aiogram.filters import StateFilter
from aiogram.types import ErrorEvent, Message

from api.client import TicketApiClient
from bot import keyboards, texts
from bot.session_sync import is_authenticated, restore_fsm_from_api, session_state
from bot.states import AuthStates
from bot.telegram_media import MainBotMediaService
from handlers import admin_panel, auth, moderation, monitoring, start, withdrawals
from handlers.common import (
    load_session,
    process_login_text,
    process_password_text,
    reset_auth_flow,
    route_guest_message,
    show_guest_home,
)
from handlers.moderation import present_current_ticket
from handlers.withdrawals import show_moderation_hub
from middlewares import ApiClientMiddleware, ApiErrorMiddleware, SessionRecoveryMiddleware

logger = logging.getLogger(__name__)


def setup_routers(api: TicketApiClient) -> Router:
    root = Router()
    api_error_middleware = ApiErrorMiddleware()

    root.message.middleware(api_error_middleware)
    root.callback_query.middleware(api_error_middleware)

    root.include_router(start.router)
    root.include_router(auth.router)
    root.include_router(admin_panel.router)
    root.include_router(moderation.router)
    root.include_router(withdrawals.router)
    root.include_router(monitoring.router)

    @root.message(F.text, ~F.text.startswith("/"), StateFilter(None))
    async def fallback_text(
        message: Message,
        api: TicketApiClient,
        main_bot_media: MainBotMediaService,
        state,
    ) -> None:
        telegram_id = message.from_user.id

        if await route_guest_message(message, state, api):
            return

        session = await load_session(api, telegram_id)
        await restore_fsm_from_api(state, api, telegram_id, session)

        if session and is_authenticated(session):
            api_state = session_state(session)
            if api_state == "moderating":
                await present_current_ticket(
                    message,
                    api,
                    telegram_id,
                    main_bot_media=main_bot_media,
                    state=state,
                )
                return
            await message.answer(texts.START_AUTH, reply_markup=keyboards.admin_reply_keyboard())
            await show_moderation_hub(message, state)
            return

        if session and session_state(session) == "awaiting_login":
            await state.set_state(AuthStates.awaiting_login)
            await message.answer(texts.ASK_LOGIN)
            return

        if session and session_state(session) == "awaiting_password":
            payload = session.get("payload") or {}
            login = str(payload.get("login", "")).strip()
            if login:
                await reset_auth_flow(api, state, telegram_id, message=message, ask_login=True)
                return
            await state.set_state(AuthStates.awaiting_login)
            await message.answer(texts.ASK_LOGIN)
            return

        await show_guest_home(message)

    return root


async def log_errors(event: ErrorEvent) -> None:
    logger.exception("Ошибка обработки update: %s", event.exception)
