import asyncio
import logging
import sys

import aiohttp
from aiogram import Bot, Dispatcher
from aiogram.client.default import DefaultBotProperties
from aiogram.client.session.aiohttp import AiohttpSession
from aiogram.enums import ParseMode
from aiogram.exceptions import TelegramNetworkError
from aiogram.fsm.storage.memory import MemoryStorage
from aiogram.types import User

from api.client import ApiError, TicketApiClient
from api.main_bot_notify_client import MainBotNotifyClient
from api.writing_off_sparks_client import WritingOffSparksGrpcClient
from bot.alert_delivery import AlertDeliveryService
from bot.http_session import create_client_session
from bot.telegram_media import MainBotMediaService
from config import Settings
from handlers import log_errors, setup_routers
from middlewares import (
    ApiClientMiddleware,
    MainBotMediaMiddleware,
    MainBotNotifyMiddleware,
    SessionRecoveryMiddleware,
    WritingOffSparksMiddleware,
)

logging.basicConfig(level=logging.INFO, stream=sys.stdout)

NETWORK_HELP = """
Не удалось подключиться к api.telegram.org.

Проверьте:
1. TELEGRAM_PROXY в .env (тот же, что у основного LooksRating-бота)
   Пример: TELEGRAM_PROXY=socks5://127.0.0.1:1080
   В Docker localhost заменяется на host.docker.internal автоматически.

2. Для SOCKS установлен пакет aiohttp-socks (в образе ticket-bot).

3. Прокси-сервер запущен и доступен с хоста/контейнера.
"""


def _build_bot(settings: Settings) -> Bot:
    session = AiohttpSession(
        proxy=settings.telegram_proxy,
        timeout=settings.telegram_request_timeout_seconds,
    )
    return Bot(
        token=settings.bot_token,
        session=session,
        default=DefaultBotProperties(parse_mode=ParseMode.HTML),
    )


async def _verify_main_bot_token(settings: Settings) -> None:
    url = f"https://api.telegram.org/bot{settings.source_bot_token}/getMe"
    try:
        async with create_client_session(
            proxy=settings.telegram_proxy,
            timeout_seconds=15,
        ) as session:
            async with session.get(url) as response:
                payload = await response.json()
    except (aiohttp.ClientError, OSError, TimeoutError) as exc:
        logging.error(
            "Не удалось проверить LOOKS_RATING_BOT_TOKEN: %s. "
            "Фото в модерации не будут загружаться.",
            exc,
        )
        return

    if not payload.get("ok"):
        logging.error(
            "LOOKS_RATING_BOT_TOKEN отклонён Telegram: %s. "
            "Фото в модерации не будут загружаться.",
            payload.get("description") or "getMe failed",
        )
        return

    username = (payload.get("result") or {}).get("username") or "?"
    logging.info("Main bot token OK for moderation photos: @%s", username)


async def _connect_bot(bot: Bot, settings: Settings) -> User:
    last_error: TelegramNetworkError | None = None
    for attempt in range(1, settings.telegram_startup_retries + 1):
        try:
            return await bot.get_me()
        except TelegramNetworkError as exc:
            last_error = exc
            if attempt >= settings.telegram_startup_retries:
                break
            delay = min(2 ** attempt, 30)
            logging.warning(
                "Telegram getMe attempt %s/%s failed: %s. Retry in %ss",
                attempt,
                settings.telegram_startup_retries,
                exc,
                delay,
            )
            await asyncio.sleep(delay)

    assert last_error is not None
    raise last_error


async def main() -> None:
    settings = Settings.from_env()
    if settings.telegram_proxy:
        logging.info("Telegram proxy: %s", settings.telegram_proxy)
    else:
        logging.warning(
            "TELEGRAM_PROXY не задан — нужен прямой доступ к api.telegram.org"
        )

    api = TicketApiClient(
        settings.api_base_url,
        settings.api_key,
        timeout_seconds=settings.api_timeout_seconds,
    )
    main_bot_media = MainBotMediaService(
        settings.source_bot_token,
        proxy=settings.telegram_proxy,
        timeout_seconds=settings.media_timeout_seconds,
    )
    writing_off_sparks = WritingOffSparksGrpcClient(
        settings.api_grpc_address,
        timeout=settings.api_timeout_seconds,
        api_key=settings.looks_rating_api_key,
    )
    main_bot_notify = MainBotNotifyClient(
        settings.main_bot_notify_base_url,
        settings.main_bot_notify_api_key,
        timeout_seconds=settings.main_bot_notify_timeout_seconds,
    )
    await api.start()
    await main_bot_media.start()
    await _verify_main_bot_token(settings)
    try:
        await api.check_connection()
        logging.info("Ticket API reachable: %s", settings.api_base_url)
    except ApiError as exc:
        logging.error(
            "Ticket API rejected bot on startup (HTTP %s): %s. "
            "Проверьте TICKET_API_KEY в .env.",
            exc.status,
            exc.message,
        )
    except (aiohttp.ClientError, OSError) as exc:
        logging.error(
            "Ticket API недоступен на старте (%s): %s",
            settings.api_base_url,
            exc,
        )

    bot = _build_bot(settings)
    alert_delivery = AlertDeliveryService(api, bot, interval_seconds=30)
    dp = Dispatcher(storage=MemoryStorage())
    dp.update.outer_middleware(ApiClientMiddleware(api))
    dp.update.outer_middleware(WritingOffSparksMiddleware(writing_off_sparks))
    dp.update.outer_middleware(MainBotNotifyMiddleware(main_bot_notify))
    dp.update.outer_middleware(MainBotMediaMiddleware(main_bot_media))
    dp.update.outer_middleware(SessionRecoveryMiddleware())
    dp.errors.register(log_errors)
    dp.include_router(setup_routers(api))

    try:
        try:
            me = await _connect_bot(bot, settings)
            logging.info("Ticket bot connected: @%s", me.username)
            await alert_delivery.start()
        except TelegramNetworkError:
            logging.error(NETWORK_HELP.strip())
            raise

        await dp.start_polling(bot)
    finally:
        await alert_delivery.stop()
        await api.close()
        await main_bot_media.close()
        await bot.session.close()


if __name__ == "__main__":
    asyncio.run(main())
