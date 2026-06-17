from __future__ import annotations

import config


class TestConfigModes:
    def test_is_gift_grpc_mode(self, monkeypatch) -> None:
        monkeypatch.setattr(config, "APP_MODE", "gift_grpc")
        assert config.is_gift_grpc_mode() is True
        assert config.is_detector_mode() is False

    def test_is_detector_mode(self, monkeypatch) -> None:
        monkeypatch.setattr(config, "APP_MODE", "detector")
        assert config.is_detector_mode() is True
        assert config.is_gift_grpc_mode() is False

    def test_is_vip_scheduler_mode_requires_flag(self, monkeypatch) -> None:
        monkeypatch.setattr(config, "APP_MODE", "vip_scheduler")
        monkeypatch.setattr(config, "VIP_GIFT_JOB_ENABLED", True)
        assert config.is_vip_scheduler_mode() is True

        monkeypatch.setattr(config, "VIP_GIFT_JOB_ENABLED", False)
        assert config.is_vip_scheduler_mode() is False

    def test_vip_gift_ids_parses_csv(self, monkeypatch) -> None:
        monkeypatch.setattr(config, "VIP_GIFT_IDS_RAW", "10, 20,30")
        assert config.vip_gift_ids() == [10, 20, 30]

    def test_vip_gift_ids_empty_when_unset(self, monkeypatch) -> None:
        monkeypatch.setattr(config, "VIP_GIFT_IDS_RAW", "")
        monkeypatch.setattr(config, "GIFT_IDS", [])
        assert config.vip_gift_ids() == []
