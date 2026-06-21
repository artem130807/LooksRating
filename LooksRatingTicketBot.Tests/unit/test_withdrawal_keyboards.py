"""Клавиатуры для заявок на вывод."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "LooksRatingTicketBot"))

from bot.keyboards import (  # noqa: E402
    CALLBACK_MOD_HUB_COMPLAINTS,
    CALLBACK_MOD_HUB_WITHDRAWALS,
    CALLBACK_PREFIX_WITHDRAWAL_OPEN,
    moderation_hub,
    withdrawal_detail_actions,
)


def test_moderation_hub_has_complaints_and_withdrawals() -> None:
    markup = moderation_hub()
    callbacks = {
        button.callback_data
        for row in markup.inline_keyboard
        for button in row
    }

    assert CALLBACK_MOD_HUB_COMPLAINTS in callbacks
    assert CALLBACK_MOD_HUB_WITHDRAWALS in callbacks


def test_withdrawal_detail_actions_contains_status_buttons() -> None:
    markup = withdrawal_detail_actions("request-id-1")
    callbacks = {
        button.callback_data
        for row in markup.inline_keyboard
        for button in row
    }

    assert "wos:exit:request-id-1" in callbacks
    assert "wos:done:request-id-1" in callbacks
    assert "wos:cancel:request-id-1" in callbacks
    assert f"{CALLBACK_PREFIX_WITHDRAWAL_OPEN}request-id-1" not in callbacks


def test_withdrawal_detail_actions_readonly_hides_status_buttons() -> None:
    markup = withdrawal_detail_actions("request-id-1", allow_status_change=False)
    callbacks = {
        button.callback_data
        for row in markup.inline_keyboard
        for button in row
    }

    assert callbacks == {"wos:exit:request-id-1"}
