from __future__ import annotations

import asyncio
import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from api.client import ApiError, LooksRatingApiClient
from bot.keyboards import top_notification_keyboard
from bot import texts

logger = logging.getLogger(__name__)


class TopNotificationsService:
    def __init__(
        self,
        api: LooksRatingApiClient,
        bot: Bot,
        interval_seconds: int,
    ) -> None:
        self._api = api
        self._bot = bot
        self._interval_seconds = max(10, interval_seconds)
        self._notified_ids: set[int] = set()
        self._last_top_signature: tuple[int, ...] = ()
        self._task: asyncio.Task | None = None
        self._stop_event = asyncio.Event()

    async def start(self) -> None:
        if self._task and not self._task.done():
            return
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run(), name="top-notifications-loop")

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
                logger.exception("Top notifications tick failed")
            try:
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                continue

    async def _tick(self) -> None:
        try:
            ids = await self._api.get_the_best_week_photos_ids()
        except ApiError as exc:
            logger.warning("Top notifications API error: %s (%s)", exc.message, exc.status)
            return

        current_ids = {int(item) for item in ids if int(item) > 0}
        current_signature = tuple(sorted(current_ids))

        if not current_ids:
            self._notified_ids.clear()
            self._last_top_signature = ()
            return

        if current_signature != self._last_top_signature:
            self._notified_ids.clear()
            self._last_top_signature = current_signature

        fresh_ids = [item for item in current_ids if item not in self._notified_ids]

        for telegram_id in fresh_ids:
            try:
                user = await self._api.get_user(telegram_id)
                display_name = "участник"
                if isinstance(user, dict):
                    candidate = str(user.get("name") or "").strip()
                    if candidate:
                        display_name = candidate

                await self._bot.send_message(
                    chat_id=telegram_id,
                    text=texts.TOP_NOTIFY_TEXT.format(name=display_name),
                    reply_markup=top_notification_keyboard(),
                )
                self._notified_ids.add(telegram_id)
            except (TelegramForbiddenError, TelegramBadRequest):
                self._notified_ids.add(telegram_id)
            except Exception:
                logger.exception("Failed to notify user %s", telegram_id)
