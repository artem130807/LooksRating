from __future__ import annotations

import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError
from aiogram.types import InlineKeyboardMarkup

from api.client import LooksRatingApiClient
from bot import texts
from bot.html_text import escape_telegram_html
from bot.keyboards import rating_message_notification_keyboard
from services.rating_user_message_models import PendingRatingUserMessage
from services.rating_user_message_protocol import RatingUserMessageStore
from services.rating_user_message_rate_limit_protocol import (
    AllowAllRatingUserMessageRateLimiter,
    RatingMessageRateLimitBlock,
    RatingUserMessageRateLimiter,
)

logger = logging.getLogger(__name__)

MIN_MESSAGE_LENGTH = 1
MAX_MESSAGE_LENGTH = 500


def validate_message_text(text: str | None) -> str | None:
    normalized = (text or "").strip()
    if len(normalized) < MIN_MESSAGE_LENGTH:
        return None
    if len(normalized) > MAX_MESSAGE_LENGTH:
        return None
    return normalized


async def resolve_sender_display_name(
    api: LooksRatingApiClient,
    *,
    sender_telegram_id: int,
    fallback_username: str | None,
) -> str:
    try:
        user = await api.get_user(sender_telegram_id)
    except Exception:
        logger.debug("Could not load sender profile for telegram_id=%s", sender_telegram_id, exc_info=True)
        user = None

    if user:
        display_name = user.get("displayName") or user.get("DisplayName")
        if display_name:
            return str(display_name).strip()
        telegram_username = user.get("telegramUsername") or user.get("TelegramUsername")
        if telegram_username:
            username = str(telegram_username).strip().lstrip("@")
            if username:
                return f"@{username}"

    if fallback_username:
        username = fallback_username.strip().lstrip("@")
        if username:
            return f"@{username}"

    return "Участник"


class RatingUserMessageService:
    def __init__(
        self,
        bot: Bot,
        store: RatingUserMessageStore,
        rate_limiter: RatingUserMessageRateLimiter | None = None,
    ) -> None:
        self._bot = bot
        self._store = store
        self._rate_limiter = rate_limiter or AllowAllRatingUserMessageRateLimiter()

    @property
    def store(self) -> RatingUserMessageStore:
        return self._store

    async def send_message(
        self,
        api: LooksRatingApiClient,
        *,
        sender_telegram_id: int,
        sender_username: str | None,
        recipient_telegram_id: int,
        text: str,
    ) -> tuple[bool, str]:
        if recipient_telegram_id <= 0:
            return False, texts.RATING_MESSAGE_RECIPIENT_UNAVAILABLE
        if sender_telegram_id == recipient_telegram_id:
            return False, texts.RATING_MESSAGE_SELF_FORBIDDEN

        validated = validate_message_text(text)
        if validated is None:
            return False, texts.RATING_MESSAGE_INVALID_LENGTH.format(
                min_length=MIN_MESSAGE_LENGTH,
                max_length=MAX_MESSAGE_LENGTH,
            )

        block_reason = await self._rate_limiter.check_delivery(
            sender_telegram_id=sender_telegram_id,
            recipient_telegram_id=recipient_telegram_id,
        )
        if block_reason is RatingMessageRateLimitBlock.PAIR:
            return False, texts.RATING_MESSAGE_PAIR_RATE_LIMITED
        if block_reason is RatingMessageRateLimitBlock.SENDER:
            return False, texts.RATING_MESSAGE_SENDER_RATE_LIMITED

        sender_display_name = await resolve_sender_display_name(
            api,
            sender_telegram_id=sender_telegram_id,
            fallback_username=sender_username,
        )
        pending = await self._store.save(
            recipient_telegram_id=recipient_telegram_id,
            sender_telegram_id=sender_telegram_id,
            sender_display_name=sender_display_name,
            text=validated,
        )

        notification_text = texts.RATING_MESSAGE_RECEIVED_NOTIFICATION.format(
            sender_name=escape_telegram_html(sender_display_name),
        )
        keyboard: InlineKeyboardMarkup = rating_message_notification_keyboard(pending.token)

        try:
            await self._bot.send_message(
                chat_id=recipient_telegram_id,
                text=notification_text,
                reply_markup=keyboard,
            )
        except TelegramForbiddenError:
            await self._store.remove(pending.token)
            return False, texts.RATING_MESSAGE_RECIPIENT_BLOCKED_BOT
        except TelegramBadRequest:
            await self._store.remove(pending.token)
            return False, texts.RATING_MESSAGE_RECIPIENT_UNAVAILABLE
        except Exception:
            logger.exception(
                "Failed to deliver rating message from %s to %s",
                sender_telegram_id,
                recipient_telegram_id,
            )
            await self._store.remove(pending.token)
            return False, texts.RATING_MESSAGE_DELIVERY_FAILED

        await self._rate_limiter.record_delivery(
            sender_telegram_id=sender_telegram_id,
            recipient_telegram_id=recipient_telegram_id,
        )
        return True, texts.RATING_MESSAGE_SENT

    async def get_pending_for_recipient(
        self,
        token: str,
        *,
        recipient_telegram_id: int,
    ) -> PendingRatingUserMessage | None:
        pending = await self._store.get(token)
        if pending is None:
            return None
        if pending.recipient_telegram_id != recipient_telegram_id:
            return None
        return pending

    async def get_pending(self, token: str) -> PendingRatingUserMessage | None:
        return await self._store.get(token)

    async def dismiss(self, token: str) -> None:
        await self._store.remove(token)


def build_rating_user_message_service(
    bot: Bot,
    store: RatingUserMessageStore,
    rate_limiter: RatingUserMessageRateLimiter | None = None,
) -> RatingUserMessageService:
    return RatingUserMessageService(bot, store, rate_limiter)
