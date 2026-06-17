"""Регрессия: кнопки нижней панели не должны перехватываться подсказкой «выберите город»."""

import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BOT_ROOT = ROOT / "LooksRatingTicketBot"
sys.path.insert(0, str(BOT_ROOT))

from bot.keyboards import (  # noqa: E402
    ADMIN_PANEL_BUTTONS,
    BTN_CITIES,
    BTN_CURRENT,
    BTN_HELP,
    BTN_LOGOUT,
    BTN_OPS,
)


def test_monitoring_button_is_routed_via_admin_panel():
    assert BTN_OPS in ADMIN_PANEL_BUTTONS


def test_all_reply_panel_buttons_excluded_from_city_pick_hint():
    """handlers/moderation.on_selecting_city_unknown_text игнорирует эти кнопки."""
    expected = {BTN_CITIES, BTN_CURRENT, BTN_OPS, BTN_HELP, BTN_LOGOUT}
    assert expected <= ADMIN_PANEL_BUTTONS
