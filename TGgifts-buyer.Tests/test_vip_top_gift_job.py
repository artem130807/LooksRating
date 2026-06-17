from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock

import pytest

import config
from helpers.profile_builders import make_category_profiles
from jobs import vip_top_gift_job


@pytest.mark.asyncio
class TestRunVipTopGiftJob:
    async def test_skips_when_grpc_fails(self, monkeypatch, tmp_path) -> None:
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", tmp_path / "state.json")
        monkeypatch.setattr(
            vip_top_gift_job,
            "fetch_vip_top_profiles",
            MagicMock(side_effect=RuntimeError("grpc down")),
        )
        resolve = AsyncMock()
        monkeypatch.setattr(vip_top_gift_job, "resolve_gift_ids_by_price", resolve)
        dispatch = AsyncMock()
        monkeypatch.setattr(vip_top_gift_job, "dispatch_ranked_gifts", dispatch)

        await vip_top_gift_job.run_vip_top_gift_job(app=object())

        resolve.assert_not_awaited()
        dispatch.assert_not_awaited()
        assert not (tmp_path / "state.json").exists()

    async def test_saves_state_when_no_recipients(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        monkeypatch.setattr(
            vip_top_gift_job,
            "fetch_vip_top_profiles",
            MagicMock(return_value=make_category_profiles(3)),
        )
        resolve = AsyncMock()
        monkeypatch.setattr(vip_top_gift_job, "resolve_gift_ids_by_price", resolve)
        dispatch = AsyncMock()
        monkeypatch.setattr(vip_top_gift_job, "dispatch_ranked_gifts", dispatch)

        await vip_top_gift_job.run_vip_top_gift_job(app=object())

        resolve.assert_not_awaited()
        dispatch.assert_not_awaited()
        assert state_file.exists()

    async def test_dispatches_when_recipients_and_gifts_exist(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        monkeypatch.setattr(config, "VIP_GIFT_SEND_INTRO", False)
        monkeypatch.setattr(
            vip_top_gift_job,
            "fetch_vip_top_profiles",
            MagicMock(return_value=make_category_profiles(10)),
        )
        monkeypatch.setattr(
            vip_top_gift_job,
            "resolve_gift_ids_by_price",
            AsyncMock(return_value={price: 100 + index for index, price in enumerate(vip_top_gift_job.PLACE_STAR_PRICES)}),
        )
        dispatch = AsyncMock(return_value=(5, 0))
        monkeypatch.setattr(vip_top_gift_job, "dispatch_ranked_gifts", dispatch)

        await vip_top_gift_job.run_vip_top_gift_job(app=object())

        dispatch.assert_awaited_once()
        assert state_file.exists()

    async def test_skips_dispatch_when_telegram_gifts_missing(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        monkeypatch.setattr(
            vip_top_gift_job,
            "fetch_vip_top_profiles",
            MagicMock(return_value=make_category_profiles(10)),
        )
        monkeypatch.setattr(
            vip_top_gift_job,
            "resolve_gift_ids_by_price",
            AsyncMock(return_value={400: 101}),
        )
        dispatch = AsyncMock()
        monkeypatch.setattr(vip_top_gift_job, "dispatch_ranked_gifts", dispatch)

        await vip_top_gift_job.run_vip_top_gift_job(app=object())

        dispatch.assert_not_awaited()
        assert not state_file.exists()
