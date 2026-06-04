import asyncio
import logging

from apscheduler.schedulers.asyncio import AsyncIOScheduler
from apscheduler.triggers.interval import IntervalTrigger
from pyrogram import Client
from pytz import timezone as pytz_timezone

import config
from jobs.vip_top_gift_job import is_job_due, run_vip_top_gift_job

logger = logging.getLogger(__name__)


class VipTopGiftScheduler:
    def __init__(self, app: Client):
        self._app = app
        self._scheduler = AsyncIOScheduler(timezone=pytz_timezone(config.TIMEZONE))
        self._job_lock = asyncio.Lock()

    async def _run_job_safe(self) -> None:
        if self._job_lock.locked():
            logger.warning("VIP gift job already running, skip")
            return
        async with self._job_lock:
            await run_vip_top_gift_job(self._app)

    def start(self) -> None:
        self._scheduler.add_job(
            self._run_job_safe,
            trigger=IntervalTrigger(days=config.VIP_GIFT_INTERVAL_DAYS),
            id="vip_top_gift_job",
            replace_existing=True,
            max_instances=1,
            coalesce=True,
        )
        self._scheduler.start()
        print(
            f"\033[94m[ SCHEDULER ]\033[0m VIP-рассылка каждые {config.VIP_GIFT_INTERVAL_DAYS} дн. "
            f"(APScheduler / IntervalTrigger)"
        )

    async def run_if_due(self) -> None:
        if is_job_due():
            print("\033[94m[ SCHEDULER ]\033[0m Пора выполнить VIP-рассылку (первый запуск или прошло 2 недели).")
            await self._run_job_safe()

    def shutdown(self) -> None:
        if self._scheduler.running:
            self._scheduler.shutdown(wait=False)
