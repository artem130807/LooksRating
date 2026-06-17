from __future__ import annotations

from typing import Any

from aiogram.fsm.context import FSMContext

from api.client import ApiError, TicketApiClient
from bot.states import AuthStates, ModerationStates

API_STATE_AWAITING_LOGIN = "awaiting_login"
API_STATE_AWAITING_PASSWORD = "awaiting_password"
API_STATE_AUTHENTICATED = "authenticated"
API_STATE_AWAITING_CITY = "awaiting_city"
API_STATE_MODERATING = "moderating"


def session_state(session: dict[str, Any] | None) -> str:
    if not session:
        return ""
    state = session.get("state") or session.get("State") or ""
    return str(state).strip().lower()


def session_payload(session: dict[str, Any] | None) -> dict[str, str]:
    if not session:
        return {}
    payload = session.get("payload") or session.get("Payload") or {}
    if isinstance(payload, dict):
        return {str(key): str(value) for key, value in payload.items()}
    return {}


def is_authenticated(session: dict[str, Any] | None) -> bool:
    return bool(session and session.get("isAuthenticated"))


async def fetch_session(api: TicketApiClient, telegram_id: int) -> dict[str, Any] | None:
    try:
        return await api.get_session(telegram_id)
    except ApiError:
        return None


async def restore_fsm_from_api(
    state: FSMContext,
    api: TicketApiClient,
    telegram_id: int,
    session: dict[str, Any] | None = None,
) -> dict[str, Any] | None:
    if await state.get_state() is not None:
        return session

    if session is None:
        session = await fetch_session(api, telegram_id)
    if not session:
        return None

    api_state = session_state(session)
    if api_state == API_STATE_AWAITING_LOGIN:
        await state.set_state(AuthStates.awaiting_login)
    elif api_state == API_STATE_AWAITING_PASSWORD:
        login = session_payload(session).get("login", "").strip()
        if login:
            await state.set_state(AuthStates.awaiting_password)
            await state.update_data(login=login)
        else:
            await state.set_state(AuthStates.awaiting_login)
    elif api_state == API_STATE_MODERATING:
        await state.set_state(ModerationStates.moderating)
    elif api_state in {API_STATE_AWAITING_CITY, API_STATE_AUTHENTICATED}:
        await state.set_state(ModerationStates.selecting_city)

    return session
