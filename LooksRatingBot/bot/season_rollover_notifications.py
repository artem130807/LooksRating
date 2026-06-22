from __future__ import annotations

import asyncio
import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.keyboards import season_rollover_notification_keyboard

logger = logging.getLogger(__name__)


class SeasonRolloverNotificationsService:
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
        self._task = asyncio.create_task(self._run(), name="season-rollover-notifications-loop")

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
                logger.exception("Season rollover notifications tick failed")
            try:
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                continue

    async def _tick(self) -> None:
        try:
            pending = await self._api.get_pending_season_rollover_notifications()
        except ApiError as exc:
            logger.warning("Season rollover API error: %s (%s)", exc.message, exc.status)
            return

        for item in pending:
            event_id = str(item.get("eventId") or "").strip()
            closed_season_id = str(item.get("closedSeasonId") or "").strip()
            closed_name = str(item.get("closedSeasonName") or "").strip()
            new_name = str(item.get("newSeasonName") or "").strip()
            recipient_ids = item.get("recipientTelegramIds") or []

            if not event_id or not closed_season_id:
                continue

            keyboard = season_rollover_notification_keyboard(closed_season_id)
            message_text = texts.format_season_rollover_notify_text(closed_name, new_name)

            for raw_id in recipient_ids:
                telegram_id = int(raw_id or 0)
                if telegram_id <= 0:
                    continue

                try:
                    await self._bot.send_message(
                        chat_id=telegram_id,
                        text=message_text,
                        reply_markup=keyboard,
                    )
                    await self._api.ack_season_rollover_notification(
                        event_id=event_id,
                        recipient_telegram_ids=[telegram_id],
                    )
                except (TelegramForbiddenError, TelegramBadRequest):
                    await self._api.ack_season_rollover_notification(
                        event_id=event_id,
                        recipient_telegram_ids=[telegram_id],
                    )
                except Exception:
                    logger.exception(
                        "Failed to deliver season rollover notification %s to %s",
                        event_id,
                        telegram_id,
                    )
