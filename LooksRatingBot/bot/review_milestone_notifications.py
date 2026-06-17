from __future__ import annotations

import asyncio
import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.keyboards import review_milestone_notification_keyboard

logger = logging.getLogger(__name__)


class ReviewMilestoneNotificationsService:
    def __init__(
        self,
        api: LooksRatingApiClient,
        bot: Bot,
        interval_seconds: int,
    ) -> None:
        self._api = api
        self._bot = bot
        self._interval_seconds = max(10, interval_seconds)
        self._task: asyncio.Task | None = None
        self._stop_event = asyncio.Event()

    async def start(self) -> None:
        if self._task and not self._task.done():
            return
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run(), name="review-milestone-notifications-loop")

    async def stop(self) -> None:
        self._stop_event.set()
        if self._task:
            self._task.cancel()
            try:
                await self._task
            except asyncio.CancelledError:
                pass
            self._task = None

    async def _run(self) -> None:
        while not self._stop_event.is_set():
            try:
                await self._tick()
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Review milestone notifications tick failed")
            try:
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                continue

    async def _tick(self) -> None:
        try:
            pending = await self._api.get_pending_review_milestone_notifications()
        except ApiError as exc:
            logger.warning("Review milestone API error: %s (%s)", exc.message, exc.status)
            return

        for item in pending:
            notification_id = str(item.get("id") or "").strip()
            owner_telegram_id = int(item.get("ownerTelegramId") or 0)
            if not notification_id or owner_telegram_id <= 0:
                continue

            try:
                await self._bot.send_message(
                    chat_id=owner_telegram_id,
                    text=texts.REVIEW_MILESTONE_NOTIFY_TEXT,
                    reply_markup=review_milestone_notification_keyboard(notification_id),
                )
                await self._api.ack_review_milestone_notification(notification_id)
            except (TelegramForbiddenError, TelegramBadRequest):
                await self._api.ack_review_milestone_notification(notification_id)
            except Exception:
                logger.exception(
                    "Failed to deliver review milestone notification %s to %s",
                    notification_id,
                    owner_telegram_id,
                )
