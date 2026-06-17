from __future__ import annotations

from datetime import datetime, timedelta, timezone

import config
from jobs.vip_top_gift_job import _load_last_run, _save_last_run, is_job_due


class TestVipTopGiftJobState:
    def test_is_job_due_when_state_missing(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "vip_gift_job_state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        monkeypatch.setattr(config, "VIP_GIFT_INTERVAL_DAYS", 14)

        assert is_job_due() is True

    def test_is_job_due_false_after_recent_run(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "vip_gift_job_state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        monkeypatch.setattr(config, "VIP_GIFT_INTERVAL_DAYS", 14)
        _save_last_run(datetime.now(timezone.utc) - timedelta(hours=2))

        assert is_job_due() is False

    def test_is_job_due_true_after_interval_elapsed(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "vip_gift_job_state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        monkeypatch.setattr(config, "VIP_GIFT_INTERVAL_DAYS", 14)
        _save_last_run(datetime.now(timezone.utc) - timedelta(days=15))

        assert is_job_due() is True

    def test_load_last_run_returns_none_for_invalid_json(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "vip_gift_job_state.json"
        state_file.write_text("{not-json", encoding="utf-8")
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)

        assert _load_last_run() is None

    def test_save_and_load_round_trip(self, monkeypatch, tmp_path) -> None:
        state_file = tmp_path / "vip_gift_job_state.json"
        monkeypatch.setattr(config, "VIP_GIFT_STATE_FILE", state_file)
        moment = datetime(2026, 3, 1, 12, 0, 0, tzinfo=timezone.utc)

        _save_last_run(moment)
        loaded = _load_last_run()

        assert loaded is not None
        assert loaded.astimezone(timezone.utc) == moment
