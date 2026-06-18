from handlers.rating import _photo_caption, _season_top_line


def test_season_top_line_when_position_present() -> None:
    line = _season_top_line({"seasonTopPlace": 12})

    assert "12" in line
    assert "из" not in line
    assert "Место в сезоне" in line


def test_season_top_line_when_position_missing() -> None:
    assert _season_top_line({}) == ""


def test_photo_caption_includes_season_top_before_rating() -> None:
    caption = _photo_caption(
        {
            "displayName": "Artem",
            "gender": "Мужской",
            "age": 18,
            "city": "ulyanovsk",
            "rank": "🤩",
            "rating": 10.0,
            "ratingCount": 1,
            "seasonTopPlace": 12,
        }
    )

    assert "🏆 Место в сезоне" in caption
    assert "12" in caption
    assert "из" not in caption
    assert caption.index("🏆") < caption.index("📊")
