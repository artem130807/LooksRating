from __future__ import annotations

import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from bot import texts

logger = logging.getLogger(__name__)


class WritingOffSparksUserNotifier:
    def __init__(self, bot: Bot) -> None:
        self._bot = bot

    async def notify_confirmed(self, *, telegram_id: int, stars: int) -> bool:
        if telegram_id <= 0 or stars <= 0:
            return False

        try:
            await self._bot.send_message(
                chat_id=telegram_id,
                text=texts.WRITING_OFF_SPARKS_STARS_CREDITED.format(stars=stars),
            )
            return True
        except (TelegramForbiddenError, TelegramBadRequest):
            return True
        except Exception:
            logger.exception(
                "Failed to send writing-off confirmed notification to telegram_id=%s",
                telegram_id,
            )
            return False

    async def notify_cancelled(
        self,
        *,
        telegram_id: int,
        stars: int,
        sparks: int,
    ) -> bool:
        if telegram_id <= 0 or stars <= 0 or sparks <= 0:
            return False

        try:
            await self._bot.send_message(
                chat_id=telegram_id,
                text=texts.WRITING_OFF_SPARKS_WITHDRAWAL_CANCELLED.format(
                    stars=stars,
                    sparks=sparks,
                ),
            )
            return True
        except (TelegramForbiddenError, TelegramBadRequest):
            return True
        except Exception:
            logger.exception(
                "Failed to send writing-off cancelled notification to telegram_id=%s",
                telegram_id,
            )
            return False
