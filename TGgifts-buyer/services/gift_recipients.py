from adapters.looksrating_grpc_client import LooksRatingGrpcClient
from services.vip_top_models import VipTopProfile


def fetch_vip_top_profiles() -> list[VipTopProfile]:
    import config

    client = LooksRatingGrpcClient(
        config.LOOKSRATING_GRPC_ADDRESS,
        timeout=config.LOOKSRATING_GRPC_TIMEOUT,
    )
    return client.get_vip_top_profiles()
