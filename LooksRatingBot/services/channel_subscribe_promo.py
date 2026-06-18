from __future__ import annotations

import asyncio
import logging

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from api.grpc_clients import LooksRatingGrpcClient, UsersForMessagePage
from bot import texts
from bot.keyboards import channel_subscribe_keyboard
from config import Settings

logger = logging.getLogger(__name__)


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

    async def _run(self) -> None:
        while not self._stop_event.is_set():
            try:
                await self._tick_safe()
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Channel subscribe promo tick failed")
            try:
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                continue

    async def _tick_safe(self) -> None:
        if self._tick_lock.locked():
            logger.info("Channel subscribe promo tick skipped: previous tick still running")
            return

        async with self._tick_lock:
            await self._tick()

    async def _tick(self) -> None:
        # Always page 1: the unsubscribed pool shrinks as users claim the bonus.
        page = await self._fetch_page(1)
        if not page.telegram_ids:
            return

        promo_text = texts.CHANNEL_SUBSCRIBE_PROMO.format(channel_url=self._settings.channel_url)
        keyboard = channel_subscribe_keyboard()
        sent_count = 0

        for telegram_id in page.telegram_ids:
            if telegram_id <= 0:
                continue
            try:
                await self._bot.send_message(
                    chat_id=telegram_id,
                    text=promo_text,
                    reply_markup=keyboard,
                    disable_web_page_preview=False,
                )
                sent_count += 1
            except (TelegramForbiddenError, TelegramBadRequest):
                logger.debug("Channel promo skipped for telegram_id=%s", telegram_id)
            except Exception:
                logger.exception("Failed to send channel promo to telegram_id=%s", telegram_id)

            if self._send_delay > 0:
                await asyncio.sleep(self._send_delay)

        logger.info(
            "Channel subscribe promo tick: attempted=%s, sent=%s",
            len(page.telegram_ids),
            sent_count,
        )

    async def _fetch_page(self, page: int) -> UsersForMessagePage:
        return await asyncio.to_thread(
            self._grpc.get_users_for_message,
            page,
            self._page_size,
            only_unsubscribed_channel=True,
        )
