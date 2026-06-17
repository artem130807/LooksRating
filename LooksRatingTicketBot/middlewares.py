from __future__ import annotations

import logging
from typing import Any, Awaitable, Callable

import aiohttp
from aiogram import BaseMiddleware
from aiogram.exceptions import TelegramNetworkError
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message, TelegramObject

from api.client import ApiError, TicketApiClient
from bot.session_sync import restore_fsm_from_api
from bot.telegram_media import MainBotMediaService

logger = logging.getLogger(__name__)


class ApiErrorMiddleware(BaseMiddleware):
    async def __call__(
        self,
        handler: Callable[[TelegramObject, dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: dict[str, Any],
    ) -> Any:
        try:
            return await handler(event, data)
        except ApiError as exc:
            text = exc.message or f"Ошибка API ({exc.status})"
        except (aiohttp.ClientError, OSError, TimeoutError):
            text = (
                "⚠️ Не удалось связаться с Ticket API.\n"
                "Проверьте, что ticket-api запущен, и попробуйте /start."
            )
        except TelegramNetworkError:
            text = (
                "⚠️ Нет связи с Telegram.\n"
                "Проверьте TELEGRAM_PROXY и повторите попытку."
            )

        if isinstance(event, Message):
            await event.answer(text)
        elif isinstance(event, CallbackQuery):
            await event.answer(text, show_alert=True)
        return None


class ApiClientMiddleware(BaseMiddleware):
    def __init__(self, api: TicketApiClient):
        self._api = api

    async def __call__(
        self,
        handler: Callable[[TelegramObject, dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: dict[str, Any],
    ) -> Any:
        data["api"] = self._api
        return await handler(event, data)


class MainBotMediaMiddleware(BaseMiddleware):
    def __init__(self, media: MainBotMediaService):
        self._media = media

    async def __call__(
        self,
        handler: Callable[[TelegramObject, dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: dict[str, Any],
    ) -> Any:
        data["main_bot_media"] = self._media
        return await handler(event, data)


class SessionRecoveryMiddleware(BaseMiddleware):
    @staticmethod
    def _telegram_id(event: TelegramObject) -> int | None:
        if isinstance(event, Message) and event.from_user:
            return event.from_user.id
        if isinstance(event, CallbackQuery) and event.from_user:
            return event.from_user.id
        return None

    async def __call__(
        self,
        handler: Callable[[TelegramObject, dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: dict[str, Any],
    ) -> Any:
        state: FSMContext | None = data.get("state")
        api: TicketApiClient | None = data.get("api")
        telegram_id = self._telegram_id(event)
        if state and api and telegram_id is not None:
            restored = await restore_fsm_from_api(state, api, telegram_id)
            if restored is not None:
                logger.debug(
                    "restored session for %s: state=%s auth=%s",
                    telegram_id,
                    restored.get("state"),
                    restored.get("isAuthenticated"),
                )
        return await handler(event, data)
