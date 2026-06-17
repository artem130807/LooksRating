from __future__ import annotations

import html


def escape_html(value: object) -> str:
    if value is None:
        return "—"
    text = str(value).strip()
    if not text:
        return "—"
    return html.escape(text)
