from __future__ import annotations

from bot.keyboards import (
    BTN_RATING_MESSAGE,
    CALLBACK_RATING_MESSAGE_OK_PREFIX,
    CALLBACK_RATING_MESSAGE_REPLY_PREFIX,
    CALLBACK_RATING_MESSAGE_SHOW_PREFIX,
    rating_keyboard,
    rating_message_notification_keyboard,
    rating_message_reveal_keyboard,
)


def test_rating_keyboard_includes_message_button() -> None:
    keyboard = rating_keyboard("photo-1")
    labels = [button.text for row in keyboard.inline_keyboard for button in row]
    callbacks = [button.callback_data for row in keyboard.inline_keyboard for button in row]

    assert BTN_RATING_MESSAGE in labels
    assert "msg:photo-1" in callbacks


def test_notification_keyboard_contains_show_callback() -> None:
    keyboard = rating_message_notification_keyboard("abc123")
    callback = keyboard.inline_keyboard[0][0].callback_data
    assert callback == f"{CALLBACK_RATING_MESSAGE_SHOW_PREFIX}abc123"


def test_reveal_keyboard_contains_reply_and_ok_callbacks() -> None:
    keyboard = rating_message_reveal_keyboard("abc123")
    callbacks = [button.callback_data for row in keyboard.inline_keyboard for button in row]

    assert f"{CALLBACK_RATING_MESSAGE_REPLY_PREFIX}abc123" in callbacks
    assert f"{CALLBACK_RATING_MESSAGE_OK_PREFIX}abc123" in callbacks
