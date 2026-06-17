import pytest

from api.client import ApiError
from bot import callbacks, texts
from bot.keyboards import MENU_PRIVILEGES
from handlers.privileges import (
    menu_privileges,
    privileges_hub_callback,
    privileges_referral_callback,
    privileges_vip_callback,
)
from helpers.aiogram_builders import make_callback, make_message
from helpers.fakes import FakeApiClient


@pytest.mark.asyncio
async def test_menu_privileges_shows_hub() -> None:
    api = FakeApiClient(user={"telegramId": 42_001, "hasVip": False})
    message = make_message(MENU_PRIVILEGES)

    await menu_privileges(message, api)

    message.answer.assert_awaited_once()
    text = message.answer.await_args.args[0]
    markup = message.answer.await_args.kwargs["reply_markup"]
    assert texts.PRIVILEGES_HUB in text
    assert markup.inline_keyboard[0][0].callback_data == callbacks.PRIVILEGES_VIP


@pytest.mark.asyncio
async def test_menu_privileges_requires_registered_user() -> None:
    api = FakeApiClient(user=None)
    message = make_message(MENU_PRIVILEGES)

    await menu_privileges(message, api)

    message.answer.assert_awaited_once_with(texts.NEED_START)


@pytest.mark.asyncio
async def test_privileges_vip_callback_opens_vip_shop() -> None:
    api = FakeApiClient(user={"telegramId": 42_001, "hasVip": True})
    callback = make_callback(callbacks.PRIVILEGES_VIP)

    await privileges_vip_callback(callback, api)

    callback.message.edit_text.assert_awaited_once()
    text = callback.message.edit_text.await_args.args[0]
    markup = callback.message.edit_text.await_args.kwargs["reply_markup"]
    assert texts.VIP_SHOP_MENU in text
    assert any(
        btn.callback_data == callbacks.SHOP_GIFTS
        for row in markup.inline_keyboard
        for btn in row
    )


@pytest.mark.asyncio
async def test_privileges_hub_callback_edits_message() -> None:
    api = FakeApiClient(user={"telegramId": 42_001, "hasVip": False})
    callback = make_callback(callbacks.PRIVILEGES_HUB)

    await privileges_hub_callback(callback, api)

    callback.message.edit_text.assert_awaited_once()
    assert callback.message.edit_text.await_args.args[0] == texts.PRIVILEGES_HUB
    markup = callback.message.edit_text.await_args.kwargs["reply_markup"]
    assert markup.inline_keyboard[1][0].callback_data == callbacks.PRIVILEGES_REFERRAL


@pytest.mark.asyncio
async def test_privileges_referral_callback_shows_existing_link() -> None:
    link = "https://t.me/LooksRatingBot?start=abc-def"
    api = FakeApiClient(
        user={"telegramId": 42_001, "hasVip": False},
        referral_link=link,
    )
    callback = make_callback(callbacks.PRIVILEGES_REFERRAL)

    await privileges_referral_callback(callback, api)

    callback.message.edit_text.assert_awaited_once()
    text = callback.message.edit_text.await_args.args[0]
    assert texts.REFERRAL_PROGRAM_INTRO in text
    assert link in text
    assert texts.REFERRAL_PROGRAM_LINK_EXISTING.split("{")[0].strip() in text


@pytest.mark.asyncio
async def test_privileges_referral_callback_creates_link_when_missing() -> None:
    api = FakeApiClient(
        user={"telegramId": 42_001, "hasVip": False},
        referral_link=None,
    )
    callback = make_callback(callbacks.PRIVILEGES_REFERRAL)

    await privileges_referral_callback(callback, api)

    text = callback.message.edit_text.await_args.args[0]
    assert texts.REFERRAL_PROGRAM_LINK_NEW.split("{")[0].strip() in text
    assert api.create_referral_link_calls == [42_001]


@pytest.mark.asyncio
async def test_privileges_referral_callback_handles_api_failure() -> None:
    api = FakeApiClient(
        user={"telegramId": 42_001, "hasVip": False},
        referral_link=None,
        referral_create_error=ApiError(500, message="down"),
    )
    callback = make_callback(callbacks.PRIVILEGES_REFERRAL)

    await privileges_referral_callback(callback, api)

    callback.message.edit_text.assert_awaited_once()
    assert texts.REFERRAL_PROGRAM_UNAVAILABLE in callback.message.edit_text.await_args.args[0]
