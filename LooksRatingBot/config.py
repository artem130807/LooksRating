import os
from dataclasses import dataclass

from dotenv import load_dotenv

load_dotenv()


@dataclass(frozen=True)
class Settings:
    bot_token: str
    api_base_url: str
    api_grpc_address: str
    tgifts_grpc_address: str
    grpc_timeout_seconds: float
    api_key: str
    telegram_proxy: str | None
    top_notify_interval_seconds: int
    review_notify_interval_seconds: int
    stars_provider_token: str

    @classmethod
    def from_env(cls) -> "Settings":
        token = os.getenv("BOT_TOKEN", "").strip()
        base = os.getenv("API_BASE_URL", "http://api:8080").strip().rstrip("/")
        api_grpc = os.getenv("API_GRPC_ADDRESS", "api:8080").strip()
        tgifts_grpc = os.getenv("TGIFTS_GRPC_ADDRESS", "tgifts-buyer:50051").strip()
        grpc_timeout_raw = os.getenv("GRPC_TIMEOUT_SECONDS", "60").strip()
        key = os.getenv("API_KEY", "").strip()
        proxy = os.getenv("TELEGRAM_PROXY", "").strip() or None
        interval_raw = os.getenv("TOP_NOTIFY_INTERVAL_SECONDS", "60").strip()
        review_interval_raw = os.getenv("REVIEW_NOTIFY_INTERVAL_SECONDS", "60").strip()
        stars_provider_token = os.getenv("STARS_PROVIDER_TOKEN", "").strip()
        try:
            interval = max(10, int(interval_raw))
        except ValueError:
            interval = 60
        try:
            review_interval = max(10, int(review_interval_raw))
        except ValueError:
            review_interval = 60
        try:
            grpc_timeout = max(5.0, float(grpc_timeout_raw))
        except ValueError:
            grpc_timeout = 60.0
        if not token:
            raise RuntimeError("BOT_TOKEN is not set")
        return cls(
            bot_token=token,
            api_base_url=base,
            api_grpc_address=api_grpc,
            tgifts_grpc_address=tgifts_grpc,
            grpc_timeout_seconds=grpc_timeout,
            api_key=key,
            telegram_proxy=proxy,
            top_notify_interval_seconds=interval,
            review_notify_interval_seconds=review_interval,
            stars_provider_token=stars_provider_token,
        )
