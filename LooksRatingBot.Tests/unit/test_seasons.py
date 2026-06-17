from bot import texts
from handlers.seasons import _season_card_text


def test_season_card_prefers_photo_profiles_count() -> None:
    season = {
        "name": "June",
        "number": 6,
        "photoProfilesCount": 120,
        "photoUsersCount": 999,
        "isClosed": False,
    }

    text = _season_card_text(season, current_id=None)

    assert "120" in text
    assert "999" not in text


def test_season_card_marks_current_season() -> None:
    season = {
        "id": "season-1",
        "name": "June",
        "number": 6,
        "photoProfilesCount": 10,
        "isClosed": False,
    }

    text = _season_card_text(season, current_id="season-1")

    assert texts.SEASON_CURRENT.strip() in text


def test_season_card_shows_closed_badge() -> None:
    season = {
        "name": "May",
        "number": 5,
        "photoProfilesCount": 3,
        "isClosed": True,
    }

    text = _season_card_text(season, current_id=None)

    assert texts.SEASON_CLOSED.strip() in text
