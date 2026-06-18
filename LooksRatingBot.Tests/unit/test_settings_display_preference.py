from __future__ import annotations

import pytest
from unittest.mock import AsyncMock, MagicMock

from bot import texts
from bot.keyboards import BTN_HIDE_TELEGRAM_USERNAME, BTN_SHOW_TELEGRAM_USERNAME, settings_keyboard
from bot.services import build_settings_menu_text, resolve_display_preference_action
from handlers import settings as settings_handlers


def test_resolve_display_preference_action_hide_when_username_is_public() -> None:
    user = {
        "telegramUsername": "rated_user",
        "usesTelegramUsernameAsDisplay": True,
    }

    assert resolve_display_preference_action(user) == "hide"


def test_resolve_display_preference_action_show_when_custom_name_is_public() -> None:
    user = {
        "telegramUsername": "rated_user",
        "usesTelegramUsernameAsDisplay": False,
    }

    assert resolve_display_preference_action(user) == "show"


def test_resolve_display_preference_action_none_without_username() -> None:
    user = {
        "telegramUsername": None,
        "usesTelegramUsernameAsDisplay": False,
    }

    assert resolve_display_preference_action(user) is None


def test_build_settings_menu_text_includes_hide_hint() -> None:
    text = build_settings_menu_text(
        {
            "telegramUsername": "rated_user",
            "usesTelegramUsernameAsDisplay": True,
        }
    )

    assert texts.SETTINGS_MENU in text
    assert texts.SETTINGS_MENU_HIDE_USERNAME in text
    assert texts.SETTINGS_MENU_SHOW_USERNAME not in text


def test_build_settings_menu_text_includes_show_hint() -> None:
    text = build_settings_menu_text(
        {
            "telegramUsername": "rated_user",
            "usesTelegramUsernameAsDisplay": False,
        }
    )

    assert texts.SETTINGS_MENU_SHOW_USERNAME in text
    assert texts.SETTINGS_MENU_HIDE_USERNAME not in text


def test_settings_keyboard_adds_display_preference_buttons() -> None:
    hide_markup = settings_keyboard(display_preference_action="hide")
    show_markup = settings_keyboard(display_preference_action="show")

    hide_labels = {button.text for row in hide_markup.keyboard for button in row}
    show_labels = {button.text for row in show_markup.keyboard for button in row}

    assert BTN_HIDE_TELEGRAM_USERNAME in hide_labels
    assert BTN_SHOW_TELEGRAM_USERNAME not in hide_labels
    assert BTN_SHOW_TELEGRAM_USERNAME in show_labels
    assert BTN_HIDE_TELEGRAM_USERNAME not in show_labels


@pytest.mark.asyncio
async def test_show_telegram_username_updates_preference(monkeypatch: pytest.MonkeyPatch) -> None:
    api = AsyncMock()
    api.get_user.return_value = {
        "telegramUsername": "rated_user",
        "usesTelegramUsernameAsDisplay": False,
    }
    api.update_display_preference.return_value = {
        "displayName": "@rated_user",
        "usesTelegramUsernameAsDisplay": True,
    }
    send_settings_menu = AsyncMock()
    monkeypatch.setattr(settings_handlers, "send_settings_menu", send_settings_menu)

    message = MagicMock()
    message.from_user.id = 101
    message.from_user.username = "rated_user"
    state = AsyncMock()

    await settings_handlers.show_telegram_username(message, state, api)

    api.update_display_preference.assert_awaited_once_with(
        101,
        telegram_username="rated_user",
        use_telegram_username_as_display=True,
    )
    send_settings_menu.assert_awaited_once()


@pytest.mark.asyncio
async def test_hide_telegram_username_save_updates_preference(monkeypatch: pytest.MonkeyPatch) -> None:
    api = AsyncMock()
    api.update_display_preference.return_value = {
        "displayName": "Мария",
        "usesTelegramUsernameAsDisplay": False,
    }
    send_settings_menu = AsyncMock()
    monkeypatch.setattr(settings_handlers, "send_settings_menu", send_settings_menu)

    message = MagicMock()
    message.text = "Мария"
    message.from_user.id = 102
    message.from_user.username = "rated_user"
    state = AsyncMock()

    await settings_handlers.hide_telegram_username_save(message, state, api)

    api.update_display_preference.assert_awaited_once_with(
        102,
        telegram_username="rated_user",
        use_telegram_username_as_display=False,
        custom_name="Мария",
    )
    state.clear.assert_awaited_once()
    send_settings_menu.assert_awaited_once()
