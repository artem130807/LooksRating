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
    channel_username: str
    channel_url: str
    channel_promo_interval_seconds: int
    channel_promo_page_size: int
    channel_promo_send_delay_seconds: float
    channel_promo_enabled: bool

    @classmethod
    def from_env(cls) -> "Settings":
        token = os.getenv("BOT_TOKEN", "").strip()
        base = os.getenv("API_BASE_URL", "http://api:8080").strip().rstrip("/")
        api_grpc = os.getenv("API_GRPC_ADDRESS", "api:8081").strip()
        tgifts_grpc = os.getenv("TGIFTS_GRPC_ADDRESS", "tgifts-buyer:50051").strip()
        grpc_timeout_raw = os.getenv("GRPC_TIMEOUT_SECONDS", "60").strip()
        key = os.getenv("API_KEY", "").strip()
        proxy = os.getenv("TELEGRAM_PROXY", "").strip() or None
        interval_raw = os.getenv("TOP_NOTIFY_INTERVAL_SECONDS", "60").strip()
        review_interval_raw = os.getenv("REVIEW_NOTIFY_INTERVAL_SECONDS", "60").strip()
        stars_provider_token = os.getenv("STARS_PROVIDER_TOKEN", "").strip()
        channel_username = os.getenv("CHANNEL_USERNAME", "LooksRatingBotOfficial").strip().lstrip("@")
        channel_url = os.getenv("CHANNEL_URL", "https://t.me/LooksRatingBotOfficial").strip()
        channel_promo_interval_raw = os.getenv("CHANNEL_PROMO_INTERVAL_SECONDS", "7200").strip()
        channel_promo_page_size_raw = os.getenv("CHANNEL_PROMO_PAGE_SIZE", "100").strip()
        channel_promo_send_delay_raw = os.getenv("CHANNEL_PROMO_SEND_DELAY_SECONDS", "0.05").strip()
        channel_promo_enabled_raw = os.getenv("CHANNEL_PROMO_ENABLED", "true").strip().lower()
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
        try:
            channel_promo_interval = max(60, int(channel_promo_interval_raw))
        except ValueError:
            channel_promo_interval = 7200
        try:
            channel_promo_page_size = max(1, min(500, int(channel_promo_page_size_raw)))
        except ValueError:
            channel_promo_page_size = 100
        try:
            channel_promo_send_delay = max(0.0, float(channel_promo_send_delay_raw))
        except ValueError:
            channel_promo_send_delay = 0.05
        channel_promo_enabled = channel_promo_enabled_raw not in {"0", "false", "no", "off"}
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
            channel_username=channel_username,
            channel_url=channel_url,
            channel_promo_interval_seconds=channel_promo_interval,
            channel_promo_page_size=channel_promo_page_size,
            channel_promo_send_delay_seconds=channel_promo_send_delay,
            channel_promo_enabled=channel_promo_enabled,
        )
