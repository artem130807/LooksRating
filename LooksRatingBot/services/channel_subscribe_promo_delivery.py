from __future__ import annotations

import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from bot import texts
from bot.keyboards import channel_subscribe_keyboard
from config import Settings

logger = logging.getLogger(__name__)


def build_channel_subscribe_promo_text(settings: Settings) -> str:
    return texts.CHANNEL_SUBSCRIBE_PROMO.format(channel_url=settings.channel_url)


async def send_channel_subscribe_promo(
    bot: Bot,
    settings: Settings,
    telegram_id: int,
) -> bool:
    if telegram_id <= 0:
        return False

    try:
        await bot.send_message(
            chat_id=telegram_id,
            text=build_channel_subscribe_promo_text(settings),
            reply_markup=channel_subscribe_keyboard(),
            disable_web_page_preview=False,
        )
        return True
    except (TelegramForbiddenError, TelegramBadRequest):
        logger.debug("Channel subscribe promo skipped for telegram_id=%s", telegram_id)
        return False
    except Exception:
        logger.exception(
            "Failed to send channel subscribe promo to telegram_id=%s",
            telegram_id,
        )
        return False
