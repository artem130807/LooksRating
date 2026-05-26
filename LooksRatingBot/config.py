import os
from dataclasses import dataclass

from dotenv import load_dotenv

load_dotenv()


@dataclass(frozen=True)
class Settings:
    bot_token: str
    api_base_url: str
    api_key: str
    telegram_proxy: str | None
    top_notify_interval_seconds: int

    @classmethod
    def from_env(cls) -> "Settings":
        token = os.getenv("BOT_TOKEN", "").strip()
        base = os.getenv("API_BASE_URL", "http://localhost:5149").strip().rstrip("/")
        key = os.getenv("API_KEY", "").strip()
        proxy = os.getenv("TELEGRAM_PROXY", "").strip() or None
        interval_raw = os.getenv("TOP_NOTIFY_INTERVAL_SECONDS", "60").strip()
        try:
            interval = max(10, int(interval_raw))
        except ValueError:
            interval = 60
        if not token:
            raise RuntimeError("BOT_TOKEN is not set")
        return cls(
            bot_token=token,
            api_base_url=base,
            api_key=key,
            telegram_proxy=proxy,
            top_notify_interval_seconds=interval,
        )
