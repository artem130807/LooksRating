from typing import Any, Awaitable, Callable, Dict

from aiogram import BaseMiddleware
from aiogram.types import TelegramObject

from api.client import LooksRatingApiClient


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


def api_outer_middleware(api: LooksRatingApiClient):
    async def middleware(handler, event, data):
        data["api"] = api
        return await handler(event, data)

    return middleware
