from bot.keyboards import MENU_PHOTO_ADD, MENU_PHOTO_REPLACE, settings_keyboard


def test_settings_keyboard_hides_add_when_vip_profile_is_full():
    markup = settings_keyboard(
        has_photo=True,
        has_vip=True,
        photo_count=4,
        can_add_photo=False,
    )
    labels = {button.text for row in markup.keyboard for button in row}
    assert MENU_PHOTO_ADD not in labels
    assert MENU_PHOTO_REPLACE in labels


def test_settings_keyboard_shows_add_when_vip_can_add_more():
    markup = settings_keyboard(
        has_photo=True,
        has_vip=True,
        photo_count=3,
        can_add_photo=True,
    )
    labels = {button.text for row in markup.keyboard for button in row}
    assert MENU_PHOTO_ADD in labels
