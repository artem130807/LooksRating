from __future__ import annotations

from urllib.parse import quote

TELEGRAM_SHARE_URL = "https://t.me/share/url"
DEFAULT_REFERRAL_SHARE_TEXT = (
    "Присоединяйся к LooksRating — рейтинг внешности по городам! "
    "Перейди по ссылке, чтобы зарегистрироваться:"
)


def build_telegram_share_url(
    referral_link: str,
    *,
    share_text: str = DEFAULT_REFERRAL_SHARE_TEXT,
) -> str:
    """Opens Telegram's native share sheet instead of launching /start in the current chat."""
    return (
        f"{TELEGRAM_SHARE_URL}"
        f"?url={quote(referral_link, safe='')}"
        f"&text={quote(share_text, safe='')}"
    )
