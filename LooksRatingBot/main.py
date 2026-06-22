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

from api.client import ApiError, LooksRatingApiClient
from api.grpc_clients import LooksRatingGrpcClient
from bot.internal_notify_server import start_internal_notify_server
from bot.review_milestone_notifications import ReviewMilestoneNotificationsService
from bot.season_rollover_notifications import SeasonRolloverNotificationsService
from bot.top_notifications import TopNotificationsService
from bot.writing_off_sparks_user_notifier import WritingOffSparksUserNotifier
from config import Settings
from handlers import log_errors, setup_routers
from infrastructure.redis_client import close_redis_client, create_redis_client
from middlewares import (
    LooksRatingGrpcMiddleware,
    RatingUserMessageMiddleware,
    SettingsMiddleware,
    SparksExchangeMiddleware,
    build_sparks_exchange_saga,
)
from services.channel_subscribe_promo import ChannelSubscribePromoService
from services.rating_user_message_factory import (
    build_rating_user_message_rate_limiter,
    build_rating_user_message_store,
)
from services.rating_user_message_service import build_rating_user_message_service

logging.basicConfig(level=logging.INFO, stream=sys.stdout)

NETWORK_HELP = """
Не удалось подключиться к api.telegram.org.

Что сделать:
1. В PowerShell: Test-NetConnection api.telegram.org -Port 443
   Если TcpTestSucceeded = False — нужен VPN или прокси.

2. Включите VPN (системный) и запустите бота снова.

3. Либо укажите прокси в LooksRatingBot\\.env:
   TELEGRAM_PROXY=socks5://127.0.0.1:1080
   (порт смотрите в Clash / V2Ray / другом клиенте; часто 7890, 1080, 10808)

4. Установите поддержку SOCKS (один раз):
   .\\venv\\Scripts\\pip install aiohttp-socks
"""


def _build_bot(settings: Settings) -> Bot:
    session = AiohttpSession(proxy=settings.telegram_proxy) if settings.telegram_proxy else None
    return Bot(
        token=settings.bot_token,
        session=session,
        default=DefaultBotProperties(parse_mode=ParseMode.HTML),
    )


async def main() -> None:
    settings = Settings.from_env()
    api = LooksRatingApiClient(settings.api_base_url, settings.api_key)
    await api.start()
    try:
        await api.check_connection()
        logging.info("API reachable: %s", settings.api_base_url)
    except ApiError as exc:
        logging.error(
            "API rejected bot on startup (HTTP %s): %s. "
            "Проверьте, что API_KEY в .env совпадает у api и bot.",
            exc.status,
            exc.message,
        )
    except (aiohttp.ClientError, OSError) as exc:
        logging.error(
            "API недоступен на старте (%s): %s. Бот не сможет отвечать на кнопки.",
            settings.api_base_url,
            exc,
        )

    bot = _build_bot(settings)
    redis_client = None
    if settings.redis_enabled:
        try:
            redis_client = await create_redis_client(settings.redis_url)  # type: ignore[arg-type]
        except Exception:
            logging.exception("Failed to connect to Redis at %s", settings.redis_url)
            sys.exit(1)

    rating_message_store = build_rating_user_message_store(settings, redis_client)
    rating_message_rate_limiter = build_rating_user_message_rate_limiter(settings, redis_client)
    rating_user_message_service = build_rating_user_message_service(
        bot,
        rating_message_store,
        rating_message_rate_limiter,
    )
    notifications = TopNotificationsService(
        api=api,
        bot=bot,
        interval_seconds=settings.top_notify_interval_seconds,
    )
    review_notifications = ReviewMilestoneNotificationsService(
        api=api,
        bot=bot,
        interval_seconds=settings.review_notify_interval_seconds,
    )
    season_rollover_notifications = SeasonRolloverNotificationsService(
        api=api,
        bot=bot,
        interval_seconds=settings.season_rollover_notify_interval_seconds,
    )
    writing_off_user_notifier = WritingOffSparksUserNotifier(bot)
    channel_promo = ChannelSubscribePromoService(settings=settings, bot=bot)
    if settings.telegram_proxy:
        logging.info("Telegram proxy: %s", settings.telegram_proxy)
    else:
        logging.warning("TELEGRAM_PROXY не задан — нужен прямой доступ к api.telegram.org")

    internal_notify_runner = None
    try:
        try:
            me = await bot.get_me()
            logging.info("Bot connected: @%s", me.username)
        except TelegramNetworkError:
            print(NETWORK_HELP, file=sys.stderr)
            sys.exit(1)

        dp = Dispatcher(storage=MemoryStorage())
        sparks_exchange_saga = build_sparks_exchange_saga(settings)
        grpc_client = LooksRatingGrpcClient(
            settings.api_grpc_address,
            timeout=settings.grpc_timeout_seconds,
        )
        dp.update.outer_middleware(SettingsMiddleware(settings))
        dp.update.outer_middleware(LooksRatingGrpcMiddleware(grpc_client))
        dp.update.outer_middleware(SparksExchangeMiddleware(api, sparks_exchange_saga))
        dp.update.outer_middleware(RatingUserMessageMiddleware(rating_user_message_service))
        dp.errors.register(log_errors)
        dp.include_router(setup_routers(api))

        await notifications.start()
        await review_notifications.start()
        await season_rollover_notifications.start()
        internal_notify_runner = await start_internal_notify_server(
            writing_off_user_notifier,
            host=settings.internal_notify_host,
            port=settings.internal_notify_port,
            api_key=settings.internal_notify_api_key,
        )
        await channel_promo.start()
        await dp.start_polling(bot)
    finally:
        if internal_notify_runner is not None:
            await internal_notify_runner.cleanup()
        await channel_promo.stop()
        await review_notifications.stop()
        await season_rollover_notifications.stop()
        await notifications.stop()
        await close_redis_client(redis_client)
        await api.close()
        await bot.session.close()


if __name__ == "__main__":
    asyncio.run(main())
