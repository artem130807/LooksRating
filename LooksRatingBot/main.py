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
from bot.top_notifications import TopNotificationsService
from config import Settings
from handlers import log_errors, setup_routers
from middlewares import api_outer_middleware

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

    bot = _build_bot(settings)
    notifications = TopNotificationsService(
        api=api,
        bot=bot,
        interval_seconds=settings.top_notify_interval_seconds,
    )
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
        dp.update.outer_middleware(api_outer_middleware(api))
        dp.errors.register(log_errors)
        dp.include_router(setup_routers(api))

        await notifications.start()
        await dp.start_polling(bot)
    finally:
        await notifications.stop()
        await api.close()
        await bot.session.close()


if __name__ == "__main__":
    asyncio.run(main())