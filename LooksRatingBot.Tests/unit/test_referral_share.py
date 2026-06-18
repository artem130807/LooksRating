from services.referral_share import TELEGRAM_SHARE_URL, build_telegram_share_url


def test_build_telegram_share_url_uses_telegram_share_endpoint() -> None:
    link = "https://t.me/LooksRatingBot?start=abc-def"
    share_url = build_telegram_share_url(link)

    assert share_url.startswith(f"{TELEGRAM_SHARE_URL}?")
    assert "url=https%3A%2F%2Ft.me%2FLooksRatingBot%3Fstart%3Dabc-def" in share_url
    assert "text=" in share_url
    assert link not in share_url


def test_build_telegram_share_url_does_not_point_directly_to_bot_start() -> None:
    link = "https://t.me/LooksRatingBot?start=abc-def"
    share_url = build_telegram_share_url(link)

    assert share_url != link
    assert "LooksRatingBot?start=" not in share_url.split("url=", 1)[0]
