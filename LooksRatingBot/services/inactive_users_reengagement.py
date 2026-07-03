from __future__ import annotations

import asyncio
import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from api.grpc_clients import LooksRatingGrpcClient
from bot import texts
from config import Settings

logger = logging.getLogger(__name__)


class InactiveUsersReengagementService:
    _RUN_INTERVAL_SECONDS = 3 * 24 * 60 * 60
    _BATCH_SIZE = 100
    _BATCH_PAUSE_SECONDS = 5 * 60

    def __init__(self, settings: Settings, bot: Bot) -> None:
        self._bot = bot
        self._grpc = LooksRatingGrpcClient(
            settings.api_grpc_address,
            timeout=settings.grpc_timeout_seconds,
        )
        self._task: asyncio.Task | None = None
        self._stop_event = asyncio.Event()

    async def start(self) -> None:
        if self._task and not self._task.done():
            return
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run(), name="inactive-users-reengagement-loop")

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
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._RUN_INTERVAL_SECONDS)
            except asyncio.TimeoutError:
                pass

            if self._stop_event.is_set():
                break

            try:
                await self._tick()
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Inactive users reengagement tick failed")

    async def _tick(self) -> None:
        telegram_ids = await self._fetch_unactive_users()
        if not telegram_ids:
            return

        batches = self._build_batches(telegram_ids)
        sent_total = 0
        for batch_index, batch in enumerate(batches, start=1):
            sent_total += await self._send_batch(batch, texts.INACTIVE_USERS_REENGAGEMENT_TEXT)
            if self._stop_event.is_set():
                break

            if batch_index < len(batches):
                logger.info(
                    "Inactive users reengagement batch sent: batch=%s, sent_total=%s, pausing_seconds=%s",
                    batch_index,
                    sent_total,
                    self._BATCH_PAUSE_SECONDS,
                )
                try:
                    await asyncio.wait_for(self._stop_event.wait(), timeout=self._BATCH_PAUSE_SECONDS)
                except asyncio.TimeoutError:
                    pass

        logger.info(
            "Inactive users reengagement tick completed: total_candidates=%s, sent=%s",
            len(telegram_ids),
            sent_total,
        )

    async def _fetch_unactive_users(self) -> list[int]:
        raw_ids = await asyncio.to_thread(self._grpc.get_unactive_users)
        result: list[int] = []
        seen: set[int] = set()
        for telegram_id in raw_ids:
            if telegram_id <= 0 or telegram_id in seen:
                continue
            seen.add(telegram_id)
            result.append(telegram_id)
        return result

    def _build_batches(self, telegram_ids: list[int]) -> list[list[int]]:
        return [
            telegram_ids[i:i + self._BATCH_SIZE]
            for i in range(0, len(telegram_ids), self._BATCH_SIZE)
        ]

    async def _send_batch(self, telegram_ids: list[int], text: str) -> int:
        sent = 0
        for telegram_id in telegram_ids:
            try:
                await self._bot.send_message(
                    chat_id=telegram_id,
                    text=text,
                    disable_web_page_preview=True,
                )
                sent += 1
            except (TelegramForbiddenError, TelegramBadRequest):
                logger.debug("Inactive users reengagement skipped for telegram_id=%s", telegram_id)
            except Exception:
                logger.exception(
                    "Inactive users reengagement send failed for telegram_id=%s",
                    telegram_id,
                )
        return sent
