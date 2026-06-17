from __future__ import annotations

from unittest.mock import MagicMock, patch

from adapters.looksrating_grpc_client import LooksRatingGrpcClient
from grpc_gen import get_telegramIds_pb2


class TestLooksRatingGrpcClient:
    def test_get_vip_top_profiles_maps_response(self) -> None:
        entry = get_telegramIds_pb2.VipTopProfileEntry(
            telegram_id=1001,
            city="Moscow",
            rating=9.2,
            rating_count=15,
            age=25,
            gender=1,
            created_at_unix=1_700_000_000,
        )
        response = get_telegramIds_pb2.GetTelegramIdsResponse(profiles=[entry])

        stub = MagicMock()
        stub.GetTelegramIds.return_value = response
        channel = MagicMock()
        channel.close = MagicMock()

        with patch("adapters.looksrating_grpc_client.grpc.insecure_channel", return_value=channel), patch(
            "adapters.looksrating_grpc_client.get_telegramIds_pb2_grpc.GetTelegramIdsServiceStub",
            return_value=stub,
        ):
            profiles = LooksRatingGrpcClient("localhost:8080", timeout=5.0).get_vip_top_profiles()

        assert len(profiles) == 1
        profile = profiles[0]
        assert profile.telegram_id == 1001
        assert profile.city == "Moscow"
        assert profile.rating == 9.2
        assert profile.rating_count == 15
        assert profile.age == 25
        assert profile.gender == 1
        assert profile.created_at_unix == 1_700_000_000
        channel.close.assert_called_once()

    def test_get_vip_top_profiles_skips_non_positive_telegram_ids(self) -> None:
        valid = get_telegramIds_pb2.VipTopProfileEntry(
            telegram_id=2002,
            city="spb",
            rating=8.0,
            rating_count=5,
            age=20,
            gender=2,
            created_at_unix=100,
        )
        invalid = get_telegramIds_pb2.VipTopProfileEntry(
            telegram_id=0,
            city="spb",
            rating=8.0,
            rating_count=5,
            age=20,
            gender=2,
            created_at_unix=100,
        )
        response = get_telegramIds_pb2.GetTelegramIdsResponse(profiles=[invalid, valid])

        stub = MagicMock()
        stub.GetTelegramIds.return_value = response
        channel = MagicMock()
        channel.close = MagicMock()

        with patch("adapters.looksrating_grpc_client.grpc.insecure_channel", return_value=channel), patch(
            "adapters.looksrating_grpc_client.get_telegramIds_pb2_grpc.GetTelegramIdsServiceStub",
            return_value=stub,
        ):
            profiles = LooksRatingGrpcClient("localhost:8080").get_vip_top_profiles()

        assert len(profiles) == 1
        assert profiles[0].telegram_id == 2002
