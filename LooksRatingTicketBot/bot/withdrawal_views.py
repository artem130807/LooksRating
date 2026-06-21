from __future__ import annotations

from datetime import datetime, timezone
from math import ceil
from typing import Protocol

from bot.html_escape import escape_html

OUTPUT_STATUS_PENDING = 1
OUTPUT_STATUS_CANCELLED = 2
OUTPUT_STATUS_CONFIRMED = 3
OUTPUT_STATUS_FAILED = 4

WITHDRAWAL_PAGE_SIZE = 5

_STATUS_LABELS = {
    OUTPUT_STATUS_PENDING: "Ожидает",
    OUTPUT_STATUS_CANCELLED: "Отменена",
    OUTPUT_STATUS_CONFIRMED: "Выполнена",
    OUTPUT_STATUS_FAILED: "Ошибка",
}


class WithdrawalItemLike(Protocol):
    id: str
    status: int
    telegram_id: int
    city: str
    sparks_count: int
    stars: int
    created_at_unix_seconds: int


def filter_pending_items(items: list[WithdrawalItemLike]) -> list[WithdrawalItemLike]:
    return [item for item in items if item.status == OUTPUT_STATUS_PENDING]


def paginate_items(
    items: list[WithdrawalItemLike],
    *,
    page: int,
    page_size: int = WITHDRAWAL_PAGE_SIZE,
) -> tuple[list[WithdrawalItemLike], int, bool]:
    total = len(items)
    if total == 0:
        return [], 1, False
    pages = withdrawal_page_count(total, page_size)
    resolved_page = min(max(page, 1), pages)
    start = (resolved_page - 1) * page_size
    chunk = items[start : start + page_size]
    has_next = resolved_page < pages
    return chunk, resolved_page, has_next


def withdrawal_page_count(total_count: int, page_size: int = WITHDRAWAL_PAGE_SIZE) -> int:
    if total_count <= 0:
        return 1
    return max(1, ceil(total_count / page_size))


def format_withdrawal_list_header(
    *,
    city: str,
    page: int,
    page_size: int,
    total_count: int,
) -> str:
    pages = withdrawal_page_count(total_count, page_size)
    return (
        f"<b>Заявки на вывод</b>\n"
        f"Город: <b>{escape_html(city)}</b>\n"
        f"Страница: <b>{page}</b> из <b>{pages}</b>\n"
        f"Всего ожидающих: <b>{total_count}</b>"
    )


def _format_sparks(value: int) -> str:
    return f"{value:,}".replace(",", " ")


def withdrawal_list_button_label(item: WithdrawalItemLike, *, index_on_page: int) -> str:
    return f"{index_on_page + 1}. {item.stars}★ · {_format_sparks(item.sparks_count)} искр"


def _user_line(telegram_id: int, username: str | None) -> str:
    if username:
        label = escape_html(username)
    else:
        label = f"ID {telegram_id}"
    return f'<a href="tg://user?id={telegram_id}">{label}</a>'


def _format_created_at(unix_seconds: int) -> str:
    if unix_seconds <= 0:
        return "—"
    dt = datetime.fromtimestamp(unix_seconds, tz=timezone.utc)
    return dt.strftime("%d.%m.%Y %H:%M UTC")


def format_withdrawal_detail(item: WithdrawalItemLike, *, username: str | None) -> str:
    status = _STATUS_LABELS.get(item.status, "Неизвестно")
    return (
        f"<b>Заявка на вывод</b>\n"
        f"ID: <code>{escape_html(item.id)}</code>\n"
        f"Статус: <b>{status}</b>\n"
        f"Город: <b>{escape_html(item.city)}</b>\n"
        f"Звёзды: <b>{item.stars}★</b>\n"
        f"Искры: <b>{_format_sparks(item.sparks_count)}</b>\n"
        f"Создана: {_format_created_at(item.created_at_unix_seconds)}\n\n"
        f"Пользователь: {_user_line(item.telegram_id, username)}\n\n"
        f"Откройте профиль, отправьте подарок на сумму звёзд и отметьте заявку выполненной."
    )
