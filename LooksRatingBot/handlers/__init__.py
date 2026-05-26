import logging

from aiogram import F, Router
from aiogram.filters import Command, CommandStart, StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import ErrorEvent, Message

from api.client import LooksRatingApiClient
from handlers.start_logic import handle_help, handle_menu, handle_start
from middlewares import ApiClientMiddleware

logger = logging.getLogger(__name__)


def setup_routers(api: LooksRatingApiClient) -> Router:
    from handlers.feed_setup import router as feed_setup_router
    from handlers.settings import router as settings_router
    from handlers.menu import router as menu_router
    from handlers.photo import router as photo_router
    from handlers.profile import router as profile_router
    from handlers.rating import router as rating_router
    from handlers.registration import router as registration_router
    from handlers.seasons import router as seasons_router
    from handlers.top_browse import router as top_browse_router

    root = Router()
    api_middleware = ApiClientMiddleware(api)
    root.message.middleware(api_middleware)
    root.callback_query.middleware(api_middleware)

    @root.message(CommandStart())
    @root.message(F.text.startswith("/start"))
    async def on_start(message: Message, state: FSMContext) -> None:
        await handle_start(message, state, api)

    @root.message(Command("menu"))
    @root.message(F.text.startswith("/menu"))
    async def on_menu(message: Message, state: FSMContext) -> None:
        await handle_menu(message, state, api)

    @root.message(Command("help"))
    @root.message(F.text.startswith("/help"))
    async def on_help(message: Message, state: FSMContext) -> None:
        await handle_help(message, state, api)

    root.include_router(registration_router)
    root.include_router(feed_setup_router)
    root.include_router(rating_router)
    root.include_router(top_browse_router)
    root.include_router(seasons_router)
    root.include_router(photo_router)
    root.include_router(settings_router)
    root.include_router(menu_router)
    root.include_router(profile_router)

    fallback = Router()

    @fallback.message(F.text, ~F.text.startswith("/"), StateFilter(None))
    async def fallback_text(message: Message) -> None:
        await message.answer(
            "Не понял сообщение.\n"
            "Отправьте /start — начать или войти.\n"
            "/menu — главное меню."
        )

    root.include_router(fallback)

    return root


async def log_errors(event: ErrorEvent) -> None:
    logger.exception("Ошибка обработки update: %s", event.exception)
