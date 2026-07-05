from __future__ import annotations

import asyncio
import logging
import time
from typing import Protocol

from aiogram import Bot

from api.grpc_clients import LooksRatingGrpcClient, UsersForMessagePage
from config import Settings
from services.channel_subscribe_promo_delivery import send_channel_subscribe_promo

logger = logging.getLogger(__name__)


class ChannelSubscribePromoSender(Protocol):
    async def send_after_registration(self, telegram_id: int) -> bool: ...


class ChannelSubscribePromoService:
    def __init__(self, settings: Settings, bot: Bot) -> None:
        self._settings = settings
        self._bot = bot
        self._grpc = LooksRatingGrpcClient(
            settings.api_grpc_address,
            timeout=settings.grpc_timeout_seconds,
        )
        self._interval_seconds = max(60, settings.channel_promo_interval_seconds)
        self._page_size = max(1, min(500, settings.channel_promo_page_size))
        self._send_delay = max(0.0, settings.channel_promo_send_delay_seconds)
        self._enabled = settings.channel_promo_enabled
        self._task: asyncio.Task | None = None
        self._stop_event = asyncio.Event()
        self._tick_lock = asyncio.Lock()
        self._current_page = 1
        self._promo_sent_at: dict[int, float] = {}

    async def start(self) -> None:
        if not self._enabled:
            logger.info("Channel subscribe promo disabled (CHANNEL_PROMO_ENABLED=false)")
            return
        if self._task and not self._task.done():
            return
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run(), name="channel-subscribe-promo-loop")

    async def stop(self) -> None:
        self._stop_event.set()
        if self._task:
            self._task.cancel()
            try:
                await self._task
            except asyncio.CancelledError:
                pass
            self._task = None

    async def send_after_registration(self, telegram_id: int) -> bool:
        if not self._enabled:
            return False

        sent = await send_channel_subscribe_promo(self._bot, self._settings, telegram_id)
        if sent:
            self._mark_sent(telegram_id)
            logger.info(
                "Channel subscribe promo sent after registration: telegram_id=%s",
                telegram_id,
            )
        return sent

    async def _run(self) -> None:
        while not self._stop_event.is_set():
            try:
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                pass

            if self._stop_event.is_set():
                break

            try:
                await self._tick_safe()
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Channel subscribe promo tick failed")

    async def _tick_safe(self) -> None:
        if self._tick_lock.locked():
            logger.info("Channel subscribe promo tick skipped: previous tick still running")
            return

        async with self._tick_lock:
            await self._tick()

    def _is_on_cooldown(self, telegram_id: int, *, now: float | None = None) -> bool:
        sent_at = self._promo_sent_at.get(telegram_id)
        if sent_at is None:
            return False
        current = now if now is not None else time.time()
        return current - sent_at < self._interval_seconds

    def _mark_sent(self, telegram_id: int, *, now: float | None = None) -> None:
        self._promo_sent_at[telegram_id] = now if now is not None else time.time()

    def _advance_page(self, page: UsersForMessagePage) -> None:
        if page.has_next_page:
            self._current_page += 1
            return
        self._current_page = 1

    async def _tick(self) -> None:
        page = await self._fetch_page(self._current_page)
        if not page.telegram_ids:
            self._advance_page(page)
            return

        sent_count = 0
        skipped_cooldown = 0
        now = time.time()

        for telegram_id in page.telegram_ids:
            if telegram_id <= 0:
                continue
            if self._is_on_cooldown(telegram_id, now=now):
                skipped_cooldown += 1
                continue

            sent = await send_channel_subscribe_promo(
                self._bot,
                self._settings,
                telegram_id,
            )
            if sent:
                self._mark_sent(telegram_id, now=now)
                sent_count += 1

            if self._send_delay > 0:
                await asyncio.sleep(self._send_delay)

        self._advance_page(page)

        logger.info(
            "Channel subscribe promo tick: page=%s, attempted=%s, sent=%s, skipped_cooldown=%s, next_page=%s",
            page.page,
            len(page.telegram_ids),
            sent_count,
            skipped_cooldown,
            self._current_page,
        )

    async def _fetch_page(self, page: int) -> UsersForMessagePage:
        return await asyncio.to_thread(
            self._grpc.get_users_for_message,
            page,
            self._page_size,
            only_unsubscribed_channel=True,
        )
