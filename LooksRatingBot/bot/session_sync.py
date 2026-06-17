from __future__ import annotations

import logging
from typing import Any

from aiogram.fsm.context import FSMContext
from aiogram.fsm.state import State

from api.client import ApiError, LooksRatingApiClient
from bot.services import SessionState, load_cities
from bot.states import FeedSetupStates, PhotoStates, RatingStates, RegistrationStates

logger = logging.getLogger(__name__)

_FEED_API_STATES = frozenset(
    {
        SessionState.AWAITING_FEED_CITY,
        SessionState.AWAITING_FEED_AGE,
        SessionState.AWAITING_FEED_GENDER,
    }
)

_API_STATE_TO_FSM: dict[str, State] = {
    SessionState.RATING: RatingStates.awaiting_rating,
    SessionState.AWAITING_FEED_CITY: FeedSetupStates.city,
    SessionState.AWAITING_FEED_AGE: FeedSetupStates.age,
    SessionState.AWAITING_FEED_GENDER: FeedSetupStates.gender,
    # AwaitingPhoto не маппим: во время номинации API остаётся AwaitingPhoto,
    # а FSM идёт custom_city → upload. Восстановление в confirm_create ломает загрузку.
}


def extract_session_state(session: dict[str, Any] | None) -> str | None:
    if not session:
        return None
    state = session.get("state") or session.get("State")
    if isinstance(state, str) and state.strip():
        return state.strip()
    return None


async def get_persisted_session_state(
    api: LooksRatingApiClient,
    telegram_id: int,
) -> str | None:
    try:
        session = await api.get_session(telegram_id)
    except ApiError as exc:
        logger.warning("Failed to load session for %s: %s", telegram_id, exc.message)
        return None
    return extract_session_state(session)


async def restore_fsm_from_api(
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> str | None:
    if await state.get_state() is not None:
        return None

    try:
        session = await api.get_session(telegram_id)
    except ApiError as exc:
        logger.warning("Session restore skipped for %s: %s", telegram_id, exc.message)
        return None

    api_state = extract_session_state(session)
    if not api_state or api_state in {SessionState.IDLE, SessionState.START, SessionState.REGISTERED}:
        return api_state

    fsm_state = _API_STATE_TO_FSM.get(api_state)
    if api_state == SessionState.AWAITING_DISPLAY_NAME:
        username = session.get("telegramUsername") or session.get("TelegramUsername")
        await state.set_state(
            RegistrationStates.display_choice if username else RegistrationStates.display_name
        )
        if username:
            await state.update_data(username=username)
        logger.info("Restored registration FSM for telegram_id=%s", telegram_id)
        return api_state

    if fsm_state is None:
        return api_state

    await state.set_state(fsm_state)

    if api_state in _FEED_API_STATES:
        try:
            cities = await load_cities(api)
        except ApiError as exc:
            logger.warning("Failed to load cities for session restore: %s", exc.message)
            cities = []
        await state.update_data(cities=cities, feed_setup=True)

    logger.info(
        "Restored FSM state %s from API session %s for telegram_id=%s",
        fsm_state.state,
        api_state,
        telegram_id,
    )
    return api_state


def is_rating_session(api_state: str | None) -> bool:
    return api_state == SessionState.RATING


def is_feed_setup_session(api_state: str | None) -> bool:
    return api_state in _FEED_API_STATES
