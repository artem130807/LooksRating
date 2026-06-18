import logging
from typing import Any, Awaitable, Callable, Dict

import aiohttp
from aiogram import BaseMiddleware
from aiogram.types import CallbackQuery, Message, TelegramObject

from adapters.tgifts_grpc_client import TGiftsGrpcClient
from aiogram.fsm.context import FSMContext

from api.client import ApiError, LooksRatingApiClient
from api.grpc_clients import LooksRatingSparksGrpcClient
from bot.services import format_api_error
from bot.session_sync import restore_fsm_from_api
from services.gift_purchase_saga import GiftPurchaseSagaOrchestrator

logger = logging.getLogger(__name__)


from config import Settings
from api.grpc_clients import LooksRatingGrpcClient


class SettingsMiddleware(BaseMiddleware):
    def __init__(self, settings: Settings):
        self._settings = settings

    async def __call__(
        self,
        handler: Callable[[TelegramObject, Dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: Dict[str, Any],
    ) -> Any:
        data["settings"] = self._settings
        return await handler(event, data)


class LooksRatingGrpcMiddleware(BaseMiddleware):
    def __init__(self, grpc_client: LooksRatingGrpcClient):
        self._grpc = grpc_client

    async def __call__(
        self,
        handler: Callable[[TelegramObject, Dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: Dict[str, Any],
    ) -> Any:
        data["grpc"] = self._grpc
        return await handler(event, data)


class ApiErrorMiddleware(BaseMiddleware):
    async def __call__(
        self,
        handler: Callable[[TelegramObject, Dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: Dict[str, Any],
    ) -> Any:
        try:
            return await handler(event, data)
        except ApiError as exc:
            logger.warning("API error in handler: %s (HTTP %s)", exc.message, exc.status)
            text = format_api_error(exc)
        except (aiohttp.ClientError, OSError, TimeoutError) as exc:
            logger.warning("Network error in handler: %s", exc)
            text = (
                "⚠️ Не удалось связаться с API LooksRating.\n"
                "Подождите минуту и отправьте /start."
            )

        if isinstance(event, Message):
            await event.answer(text)
        elif isinstance(event, CallbackQuery):
            await event.answer(text, show_alert=True)
        return None


class ApiClientMiddleware(BaseMiddleware):
    def __init__(self, api: LooksRatingApiClient):
        self._api = api

    async def __call__(
        self,
        handler: Callable[[TelegramObject, Dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: Dict[str, Any],
    ) -> Any:
        data["api"] = self._api
        return await handler(event, data)


class SessionRecoveryMiddleware(BaseMiddleware):
    """Restore FSM from API session when in-memory state was lost (e.g. bot restart)."""

    @staticmethod
    def _telegram_id(event: TelegramObject) -> int | None:
        if isinstance(event, Message) and event.from_user:
            return event.from_user.id
        if isinstance(event, CallbackQuery) and event.from_user:
            return event.from_user.id
        return None

    async def __call__(
        self,
        handler: Callable[[TelegramObject, Dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: Dict[str, Any],
    ) -> Any:
        state: FSMContext | None = data.get("state")
        api: LooksRatingApiClient | None = data.get("api")
        telegram_id = self._telegram_id(event)
        if state and api and telegram_id is not None:
            await restore_fsm_from_api(state, api, telegram_id)
        return await handler(event, data)


class GiftPurchaseMiddleware(BaseMiddleware):
    def __init__(
        self,
        api: LooksRatingApiClient,
        gift_purchase_saga: GiftPurchaseSagaOrchestrator,
    ):
        self._api = api
        self._gift_purchase_saga = gift_purchase_saga

    async def __call__(
        self,
        handler: Callable[[TelegramObject, Dict[str, Any]], Awaitable[Any]],
        event: TelegramObject,
        data: Dict[str, Any],
    ) -> Any:
        data["api"] = self._api
        data["gift_purchase_saga"] = self._gift_purchase_saga
        return await handler(event, data)


def build_gift_purchase_saga(settings) -> GiftPurchaseSagaOrchestrator:
    sparks_client = LooksRatingSparksGrpcClient(
        settings.api_grpc_address,
        timeout=settings.grpc_timeout_seconds,
    )
    tgifts_client = TGiftsGrpcClient(
        settings.tgifts_grpc_address,
        timeout=max(settings.grpc_timeout_seconds, 120.0),
    )
    return GiftPurchaseSagaOrchestrator(sparks_client, tgifts_client)
