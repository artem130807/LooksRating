from __future__ import annotations

import os

# Config module reads env at import time — set test defaults before any project imports.
os.environ.setdefault("API_ID", "12345")
os.environ.setdefault("API_HASH", "test_api_hash")
os.environ.setdefault("INTERVAL", "10")
os.environ.setdefault("TIMEZONE", "Europe/Moscow")
os.environ.setdefault("CHANNEL_ID", "0")
os.environ.setdefault("LANGUAGE", "EN")
os.environ.setdefault("APP_MODE", "gift_grpc")
os.environ.setdefault("VIP_GIFT_JOB_ENABLED", "false")
os.environ.setdefault("VIP_GIFT_INTERVAL_DAYS", "14")
os.environ.setdefault("MIN_GIFT_PRICE", "0")
os.environ.setdefault("MAX_GIFT_PRICE", "10000")
os.environ.setdefault("GIFT_DELAY", "0")
os.environ.setdefault("NUM_GIFTS", "1")
os.environ.setdefault("PURCHASE_NON_LIMITED_GIFTS", "false")
os.environ.setdefault("HIDE_SENDER_NAME", "true")
os.environ.setdefault("GIFT_GRPC_ENABLED", "true")
os.environ.setdefault("LOOKSRATING_GRPC_ADDRESS", "localhost:50051")
os.environ.setdefault("USE_LOOKSRATING_GRPC", "false")
