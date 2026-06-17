from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock

import pytest

import config
from scheduler.vip_gift_scheduler import VipTopGiftScheduler


@pytest.mark.asyncio
class TestVipTopGiftSchedulerAsync:
    async def test_run_if_due_executes_job(self, monkeypatch) -> None:
        monkeypatch.setattr("scheduler.vip_gift_scheduler.is_job_due", lambda: True)
        run_job = AsyncMock()
        monkeypatch.setattr("scheduler.vip_gift_scheduler.run_vip_top_gift_job", run_job)

        scheduler = VipTopGiftScheduler(app=object())
        scheduler._scheduler = MagicMock()

        await scheduler.run_if_due()

        run_job.assert_awaited_once()

    async def test_run_if_due_skips_when_not_due(self, monkeypatch) -> None:
        monkeypatch.setattr("scheduler.vip_gift_scheduler.is_job_due", lambda: False)
        run_job = AsyncMock()
        monkeypatch.setattr("scheduler.vip_gift_scheduler.run_vip_top_gift_job", run_job)

        scheduler = VipTopGiftScheduler(app=object())

        await scheduler.run_if_due()

        run_job.assert_not_awaited()

    async def test_run_job_safe_skips_when_lock_held(self, monkeypatch) -> None:
        run_job = AsyncMock()
        monkeypatch.setattr("scheduler.vip_gift_scheduler.run_vip_top_gift_job", run_job)

        scheduler = VipTopGiftScheduler(app=object())
        await scheduler._job_lock.acquire()

        await scheduler._run_job_safe()

        run_job.assert_not_awaited()
        scheduler._job_lock.release()


class TestVipTopGiftSchedulerSync:
    def test_start_registers_interval_job(self, monkeypatch) -> None:
        monkeypatch.setattr(config, "VIP_GIFT_INTERVAL_DAYS", 14)
        scheduler = VipTopGiftScheduler(app=object())
        add_job = MagicMock()
        start = MagicMock()
        scheduler._scheduler.add_job = add_job
        scheduler._scheduler.start = start

        scheduler.start()

        add_job.assert_called_once()
        kwargs = add_job.call_args.kwargs
        assert kwargs["id"] == "vip_top_gift_job"
        assert kwargs["max_instances"] == 1
        start.assert_called_once()
