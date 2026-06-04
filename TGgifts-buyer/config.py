import os
from pathlib import Path

from dotenv import load_dotenv

load_dotenv()

# =======================
# GENERAL CONFIGURATION
# =======================
SESSION: str = str(Path(__file__).parent / "data/account")
API_ID: int = int(os.getenv("API_ID"))
API_HASH: str = os.getenv("API_HASH")
DATA_FILEPATH: Path = Path(__file__).parent / "data/json/history.json"

# =========================
# BOT SETTINGS
# =========================
INTERVAL: float = float(os.getenv("INTERVAL"))
TIMEZONE: str = os.getenv("TIMEZONE")
CHANNEL_ID: int = int(os.getenv("CHANNEL_ID"))

# =========================
# GIFTS | USER INFO
# =========================

LOOKSRATING_GRPC_ADDRESS: str = os.getenv("LOOKSRATING_GRPC_ADDRESS", "localhost:8080")
USE_LOOKSRATING_GRPC: bool = os.getenv("USE_LOOKSRATING_GRPC", "false").lower() == "true"
LOOKSRATING_GRPC_TIMEOUT: float = float(os.getenv("LOOKSRATING_GRPC_TIMEOUT", "60"))

APP_MODE: str = os.getenv("APP_MODE", "both").strip().lower()
VIP_GIFT_JOB_ENABLED: bool = os.getenv("VIP_GIFT_JOB_ENABLED", "true").lower() == "true"
VIP_GIFT_INTERVAL_DAYS: int = max(1, int(os.getenv("VIP_GIFT_INTERVAL_DAYS", "14")))
VIP_GIFT_STATE_FILE: Path = Path(__file__).parent / "data/json/vip_gift_job_state.json"
VIP_GIFT_SEND_INTRO: bool = os.getenv("VIP_GIFT_SEND_INTRO", "false").lower() == "true"
STARTUP_GIFT_DISPATCH: bool = os.getenv("STARTUP_GIFT_DISPATCH", "false").lower() == "true"
VIP_GIFT_IDS_RAW: str = os.getenv("VIP_GIFT_IDS", "")


def _parse_manual_user_ids() -> list:
    result = []
    user_ids = os.getenv("USER_ID", "").split(",")
    for user_id in user_ids:
        stripped = user_id.strip()
        if not stripped:
            continue
        try:
            result.append(int(stripped))
        except ValueError:
            result.append(stripped)
    return result


USER_ID: list = _parse_manual_user_ids()
TARGET_USER_IDS: list = []


def init_target_user_ids() -> None:
    global TARGET_USER_IDS
    merged: list = []
    seen: set = set()

    def add_user_id(value) -> None:
        key = value if isinstance(value, int) else str(value).lower()
        if key in seen:
            return
        seen.add(key)
        merged.append(value)

    if USE_LOOKSRATING_GRPC:
        from adapters.looksrating_grpc_client import LooksRatingGrpcClient
        from services.vip_top_ranking import build_gift_recipients

        client = LooksRatingGrpcClient(LOOKSRATING_GRPC_ADDRESS, timeout=LOOKSRATING_GRPC_TIMEOUT)
        for recipient in build_gift_recipients(client.get_vip_top_profiles()):
            add_user_id(recipient.telegram_id)

    for user_id in USER_ID:
        add_user_id(user_id)

    TARGET_USER_IDS = merged

MIN_GIFT_PRICE: int = int(os.getenv("MIN_GIFT_PRICE"))
MAX_GIFT_PRICE: int = int(os.getenv("MAX_GIFT_PRICE"))
NUM_GIFTS: int = int(os.getenv("NUM_GIFTS"))
GIFT_SUPPLY: int = int(os.getenv("GIFT_SUPPLY")) if os.getenv("GIFT_SUPPLY") else None
GIFT_DELAY: float = float(os.getenv("GIFT_DELAY"))

PURCHASE_NON_LIMITED_GIFTS: bool = os.getenv("PURCHASE_NON_LIMITED_GIFTS").lower() == "true"
HIDE_SENDER_NAME: bool = os.getenv("HIDE_SENDER_NAME").lower() == "true"
GIFT_IDS: list[int] = os.getenv("GIFT_IDS", "").split(",") if os.getenv("GIFT_IDS") else []

# =========================
# LOCALE SETTINGS
# =========================
LANGUAGE: str = os.getenv("LANGUAGE", "EN").upper()
LANG_CODES = {
    "EN": "locales.en",
    "RU": "locales.ru",
    "UK": "locales.uk",
}

locale = __import__(LANG_CODES.get(LANGUAGE, "locales.en"), fromlist=[""])


def is_detector_mode() -> bool:
    return APP_MODE in {"detector", "both"}


def is_vip_scheduler_mode() -> bool:
    return VIP_GIFT_JOB_ENABLED and APP_MODE in {"vip_scheduler", "both", "scheduler"}


def vip_gift_ids() -> list[int]:
    raw = VIP_GIFT_IDS_RAW.strip() or os.getenv("GIFT_IDS", "")
    if not raw:
        return []
    return [int(x.strip()) for x in raw.split(",") if x.strip()]
