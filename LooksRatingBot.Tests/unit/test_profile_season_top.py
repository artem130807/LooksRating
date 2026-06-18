from bot import texts


def test_profile_season_top_place_format() -> None:
    line = texts.PROFILE_SEASON_TOP_PLACE.format(place=12)

    assert "12" in line
    assert "из" not in line
    assert "Место в сезоне" in line


def test_profile_photo_stats_includes_season_top_placeholder() -> None:
    rendered = texts.PROFILE_PHOTO_STATS.format(
        rating_line="10.0/10 · 1 оценок",
        season_top_place=texts.PROFILE_SEASON_TOP_PLACE.format(place=12),
        rank="🤩",
        city="Ульяновск",
        age=18,
        gender="Мужской",
    )

    assert "🏆 Место в сезоне" in rendered
    assert "12" in rendered
    assert "из" not in rendered
    assert rendered.index("⭐") < rendered.index("🏆")
    assert rendered.index("🏆") < rendered.index("🏅")
