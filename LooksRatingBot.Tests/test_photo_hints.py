from bot import texts
from bot.photo_hints import photo_settings_intro


def test_non_vip_replace_intro_mentions_rating_reset():
    intro = photo_settings_intro(has_vip=False, recreate=True, replace_all=False)
    assert intro == texts.PHOTO_NON_VIP_INTRO_REPLACE
    assert "обнуля" in intro.lower()


def test_vip_replace_intro_mentions_rating_preserved():
    intro = photo_settings_intro(has_vip=True, recreate=True, replace_all=False)
    assert intro == texts.PHOTO_VIP_INTRO_REPLACE
    assert "сохраняется" in intro.lower()


def test_vip_add_intro_mentions_four_photos():
    intro = photo_settings_intro(has_vip=True, recreate=False, replace_all=False)
    assert intro == texts.PHOTO_VIP_INTRO_ADD
    assert "4" in intro


def test_non_vip_replace_all_uses_replace_intro():
    intro = photo_settings_intro(has_vip=False, recreate=True, replace_all=True)
    assert intro == texts.PHOTO_NON_VIP_INTRO_REPLACE
