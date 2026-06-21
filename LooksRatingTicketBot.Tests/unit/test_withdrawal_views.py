"""TDD: логика отображения заявок на вывод искр."""

import sys
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "LooksRatingTicketBot"))

from bot.withdrawal_views import (  # noqa: E402
    OUTPUT_STATUS_CANCELLED,
    OUTPUT_STATUS_CONFIRMED,
    OUTPUT_STATUS_PENDING,
    filter_pending_items,
    format_withdrawal_detail,
    format_withdrawal_list_header,
    withdrawal_list_button_label,
    withdrawal_page_count,
    paginate_items,
)


@dataclass(frozen=True)
class Item:
    id: str
    status: int
    telegram_id: int
    city: str
    sparks_count: int
    stars: int
    created_at_unix_seconds: int


def test_filter_pending_items_keeps_only_pending() -> None:
    items = [
        Item("a", OUTPUT_STATUS_PENDING, 1, "moscow", 1200, 100, 0),
        Item("b", OUTPUT_STATUS_CONFIRMED, 2, "moscow", 2400, 200, 0),
        Item("c", OUTPUT_STATUS_CANCELLED, 3, "moscow", 1200, 100, 0),
    ]

    pending = filter_pending_items(items)

    assert [item.id for item in pending] == ["a"]


def test_withdrawal_page_count() -> None:
    assert withdrawal_page_count(total_count=0, page_size=5) == 1
    assert withdrawal_page_count(total_count=5, page_size=5) == 1
    assert withdrawal_page_count(total_count=6, page_size=5) == 2


def test_format_withdrawal_list_header() -> None:
    text = format_withdrawal_list_header(city="moscow", page=2, page_size=5, total_count=12)

    assert "moscow" in text
    assert "2" in text
    assert "12" in text


def test_withdrawal_list_button_label() -> None:
    item = Item("id-1", OUTPUT_STATUS_PENDING, 42, "kazan", 1200, 100, 1_700_000_000)
    label = withdrawal_list_button_label(item, index_on_page=0)

    assert "100★" in label
    assert "1 200" in label


def test_format_withdrawal_detail_includes_user_link_and_stars() -> None:
    item = Item(
        "abc-123",
        OUTPUT_STATUS_PENDING,
        90001,
        "moscow",
        1200,
        100,
        int(datetime(2024, 6, 1, tzinfo=timezone.utc).timestamp()),
    )

    text = format_withdrawal_detail(item, username="@gift_user")

    assert "tg://user?id=90001" in text
    assert "@gift_user" in text
    assert "100★" in text
    assert "1 200" in text
    assert "Ожидает" in text


def test_paginate_items_slices_pending_list() -> None:
    items = [
        Item(f"id-{i}", OUTPUT_STATUS_PENDING, i, "moscow", 1200, 100, 0)
        for i in range(6)
    ]

    page_items, page, has_next = paginate_items(items, page=1, page_size=5)

    assert len(page_items) == 5
    assert page == 1
    assert has_next is True

    page_items, page, has_next = paginate_items(items, page=2, page_size=5)

    assert len(page_items) == 1
    assert page == 2
    assert has_next is False


def test_format_withdrawal_detail_without_username_uses_telegram_id() -> None:
    item = Item("x", OUTPUT_STATUS_PENDING, 77, "spb", 2400, 200, 0)

    text = format_withdrawal_detail(item, username=None)

    assert "tg://user?id=77" in text
    assert "77" in text
