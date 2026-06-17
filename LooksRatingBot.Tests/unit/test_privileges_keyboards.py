from bot import callbacks, texts
from bot.keyboards import (
    MENU_PRIVILEGES,
    main_menu,
    privileges_hub_keyboard,
    referral_program_keyboard,
    vip_shop_keyboard,
)


def test_main_menu_uses_privileges_button() -> None:
    markup = main_menu()
    labels = [button.text for row in markup.keyboard for button in row]
    assert MENU_PRIVILEGES in labels
    assert "Покупки" not in labels


def test_privileges_hub_keyboard_contains_vip_and_referral() -> None:
    markup = privileges_hub_keyboard(has_vip=False)
    rows = markup.inline_keyboard

    assert rows[0][0].callback_data == callbacks.PRIVILEGES_VIP
    assert rows[1][0].callback_data == callbacks.PRIVILEGES_REFERRAL
    assert rows[2][0].text == "📱 В меню"


def test_privileges_hub_marks_active_vip() -> None:
    markup = privileges_hub_keyboard(has_vip=True)
    assert "активен" in markup.inline_keyboard[0][0].text


def test_vip_shop_keyboard_shows_gifts_only_for_vip() -> None:
    without_vip = vip_shop_keyboard(has_vip=False)
    with_vip = vip_shop_keyboard(has_vip=True)

    without_callbacks = [btn.callback_data for row in without_vip.inline_keyboard for btn in row]
    with_callbacks = [btn.callback_data for row in with_vip.inline_keyboard for btn in row]

    assert callbacks.SHOP_GIFTS not in without_callbacks
    assert callbacks.SHOP_GIFTS in with_callbacks
    assert callbacks.PRIVILEGES_HUB in without_callbacks


def test_referral_program_keyboard_includes_share_url_when_link_present() -> None:
    link = "https://t.me/LooksRatingBot?start=abc"
    markup = referral_program_keyboard(link=link)
    share_button = markup.inline_keyboard[0][0]

    assert share_button.url == link
    assert markup.inline_keyboard[1][0].callback_data == callbacks.PRIVILEGES_HUB


def test_referral_program_keyboard_without_link_has_no_share_row() -> None:
    markup = referral_program_keyboard(link=None)
    assert markup.inline_keyboard[0][0].callback_data == callbacks.PRIVILEGES_HUB


def test_privileges_hub_text_mentions_referral() -> None:
    assert "реферальная программа" in texts.PRIVILEGES_HUB.lower()
    assert "VIP" in texts.PRIVILEGES_HUB
