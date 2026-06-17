import os
from dataclasses import dataclass

from dotenv import load_dotenv

from bot.http_session import normalize_telegram_proxy

load_dotenv()


@dataclass(frozen=True)
class Settings:
    bot_token: str
    source_bot_token: str
    api_base_url: str
    api_key: str
    telegram_proxy: str | None
    api_timeout_seconds: float
    media_timeout_seconds: float
    telegram_request_timeout_seconds: float
    telegram_startup_retries: int

    @classmethod
    def from_env(cls) -> "Settings":
        token = os.getenv("TICKET_BOT_TOKEN", "").strip()
        source_token = (
            os.getenv("LOOKS_RATING_BOT_TOKEN", "").strip()
            or os.getenv("TELEGRAM_BOT_TOKEN", "").strip()
        )
        base = os.getenv("TICKET_API_BASE_URL", "http://ticket-api:8090").strip().rstrip("/")
        key = os.getenv("TICKET_API_KEY", "").strip()
        proxy = normalize_telegram_proxy(os.getenv("TELEGRAM_PROXY", "").strip() or None)
        timeout_raw = os.getenv("TICKET_API_TIMEOUT_SECONDS", "30").strip()
        media_timeout_raw = os.getenv("TICKET_MEDIA_TIMEOUT_SECONDS", "45").strip()
        telegram_timeout_raw = os.getenv("TICKET_TELEGRAM_TIMEOUT_SECONDS", "60").strip()
        startup_retries_raw = os.getenv("TICKET_TELEGRAM_STARTUP_RETRIES", "8").strip()

        if not token:
            raise RuntimeError("TICKET_BOT_TOKEN is not set")
        if not source_token:
            raise RuntimeError(
                "LOOKS_RATING_BOT_TOKEN or TELEGRAM_BOT_TOKEN is required "
                "to display profile photos in moderation"
            )
        if not key:
            raise RuntimeError("TICKET_API_KEY is not set")

        try:
            timeout = float(timeout_raw)
        except ValueError:
            timeout = 30.0

        try:
            media_timeout = float(media_timeout_raw)
        except ValueError:
            media_timeout = 45.0

        try:
            telegram_timeout = float(telegram_timeout_raw)
        except ValueError:
            telegram_timeout = 60.0

        try:
            startup_retries = int(startup_retries_raw)
        except ValueError:
            startup_retries = 8
        startup_retries = max(1, startup_retries)

        return cls(
            bot_token=token,
            source_bot_token=source_token,
            api_base_url=base,
            api_key=key,
            telegram_proxy=proxy,
            api_timeout_seconds=timeout,
            media_timeout_seconds=media_timeout,
            telegram_request_timeout_seconds=telegram_timeout,
            telegram_startup_retries=startup_retries,
        )
