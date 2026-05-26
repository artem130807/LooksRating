import logging

import aiohttp
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.services import SessionState, ensure_bot_session, send_main_menu, set_bot_state
from bot.states import RatingStates, TicketStates
from handlers.rating import exit_rating
from handlers.registration import begin_registration

logger = logging.getLogger(__name__)


async def handle_start(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    current = await state.get_state()
    if current in (
        RatingStates.awaiting_rating.state,
        TicketStates.description.state,
    ):
        await exit_rating(message, state, api, message.from_user.id)
        return

    await state.clear()
    telegram_id = message.from_user.id
    username = message.from_user.username

    try:
        await ensure_bot_session(api, telegram_id)
        user = await api.get_user(telegram_id)
    except ApiError as exc:
        logger.error("API error on /start: %s (HTTP %s)", exc.message, exc.status)
        await message.answer(
            f"⚠️ Ошибка API ({exc.status}): {exc.message}\n"
            "Если только что обновили код — перезапустите LooksRatingApi."
        )
        return
    except (aiohttp.ClientError, OSError):
        logger.exception("Network error on /start")
        await message.answer(
            "⚠️ Не удалось связаться с API LooksRating.\n"
            "Запустите API в другом терминале:\n"
            "<code>cd LooksRatingApi\n dotnet run</code>\n\n"
            "Проверьте <code>API_BASE_URL</code> в LooksRatingBot\\.env"
        )
        return

    if user:
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await send_main_menu(message, api, telegram_id, texts.WELCOME_BACK)
        return

    await begin_registration(message, state, api, telegram_id, username)


async def handle_menu(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    telegram_id = message.from_user.id
    try:
        user = await api.get_user(telegram_id)
    except (ApiError, aiohttp.ClientError, OSError):
        logger.exception("API error on /menu")
        await message.answer(
            "⚠️ API недоступен. Запустите LooksRatingApi и попробуйте снова."
        )
        return

    if not user:
        await message.answer(texts.NEED_START)
        return

    current = await state.get_state()
    if current in (
        RatingStates.awaiting_rating.state,
        TicketStates.description.state,
    ):
        await exit_rating(message, state, api, telegram_id)
        return

    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.IDLE)
    await send_main_menu(message, api, telegram_id, texts.MAIN_MENU)


async def handle_help(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    current = await state.get_state()
    if current in (
        RatingStates.awaiting_rating.state,
        TicketStates.description.state,
    ):
        await exit_rating(message, state, api, message.from_user.id)
        return
    await message.answer(texts.HELP)
