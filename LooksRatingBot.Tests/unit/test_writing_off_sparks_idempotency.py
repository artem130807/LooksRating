from __future__ import annotations

from bot.writing_off_sparks_idempotency import build_writing_off_sparks_idempotency_key


def test_build_writing_off_sparks_idempotency_key_is_stable() -> None:
    key = build_writing_off_sparks_idempotency_key(
        telegram_id=42_001,
        callback_id="callback-1",
    )

    assert key == "writing-off-sparks:42001:callback-1"
