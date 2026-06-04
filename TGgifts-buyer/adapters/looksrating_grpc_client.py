import grpc

from grpc_gen import get_telegramIds_pb2
from grpc_gen import get_telegramIds_pb2_grpc
from services.vip_top_models import VipTopProfile


class LooksRatingGrpcClient:
    def __init__(self, address: str, timeout: float = 60.0):
        self._address = address
        self._timeout = timeout

    def get_vip_top_profiles(self) -> list[VipTopProfile]:
        channel = grpc.insecure_channel(
            self._address,
            options=(("grpc.enable_http_proxy", 0),),
        )
        try:
            stub = get_telegramIds_pb2_grpc.GetTelegramIdsServiceStub(channel)
            response = stub.GetTelegramIds(
                get_telegramIds_pb2.GetTelegramIdsRequest(),
                timeout=self._timeout,
            )
            profiles: list[VipTopProfile] = []
            for entry in response.profiles:
                if entry.telegram_id <= 0:
                    continue
                profiles.append(
                    VipTopProfile(
                        telegram_id=int(entry.telegram_id),
                        city=entry.city or "",
                        rating=float(entry.rating),
                        rating_count=int(entry.rating_count),
                        age=int(entry.age),
                        gender=int(entry.gender),
                        created_at_unix=int(entry.created_at_unix),
                    )
                )
            return profiles
        finally:
            channel.close()
