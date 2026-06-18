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
from bot.review_milestone_notifications import ReviewMilestoneNotificationsService
from bot.top_notifications import TopNotificationsService
from config import Settings
from handlers import log_errors, setup_routers
from middlewares import (
    GiftPurchaseMiddleware,
    LooksRatingGrpcMiddleware,
    SettingsMiddleware,
    build_gift_purchase_saga,
)
from services.channel_subscribe_promo import ChannelSubscribePromoService

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
    channel_promo = ChannelSubscribePromoService(settings=settings, bot=bot)
    if settings.telegram_proxy:
        logging.info("Telegram proxy: %s", settings.telegram_proxy)
    else:
        logging.warning("TELEGRAM_PROXY не задан — нужен прямой доступ к api.telegram.org")

    try:
        try:
            me = await bot.get_me()
            logging.info("Bot connected: @%s", me.username)
        except TelegramNetworkError:
            print(NETWORK_HELP, file=sys.stderr)
            sys.exit(1)

        dp = Dispatcher(storage=MemoryStorage())
        gift_purchase_saga = build_gift_purchase_saga(settings)
        grpc_client = LooksRatingGrpcClient(
            settings.api_grpc_address,
            timeout=settings.grpc_timeout_seconds,
        )
        dp.update.outer_middleware(SettingsMiddleware(settings))
        dp.update.outer_middleware(LooksRatingGrpcMiddleware(grpc_client))
        dp.update.outer_middleware(GiftPurchaseMiddleware(api, gift_purchase_saga))
        dp.errors.register(log_errors)
        dp.include_router(setup_routers(api))

        await notifications.start()
        await review_notifications.start()
        await channel_promo.start()
        await dp.start_polling(bot)
    finally:
        await channel_promo.stop()
        await review_notifications.stop()
        await notifications.stop()
        await api.close()
        await bot.session.close()


if __name__ == "__main__":
    asyncio.run(main())