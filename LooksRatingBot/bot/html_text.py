from __future__ import annotations

from html import escape


def escape_telegram_html(value: str) -> str:
    return escape(value, quote=False)
