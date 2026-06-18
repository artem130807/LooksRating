from __future__ import annotations

import pytest

from bot.sparks_exchange import (
    ALLOWED_STAR_TIERS,
    SPARKS_PER_STAR,
    is_allowed_star_tier,
    sparks_cost,
    sparks_costs,
)


@pytest.mark.parametrize(
    ("star_tier", "expected"),
    [
        (100, 1200),
        (200, 2400),
        (300, 3600),
        (400, 4800),
    ],
)
def test_sparks_cost_uses_twelve_to_one_rate(star_tier: int, expected: int) -> None:
    assert sparks_cost(star_tier) == expected


@pytest.mark.parametrize("star_tier", [0, 50, 150, 500])
def test_sparks_cost_rejects_unknown_tier(star_tier: int) -> None:
    assert sparks_cost(star_tier) is None
    assert is_allowed_star_tier(star_tier) is False


def test_sparks_costs_matches_allowed_tiers() -> None:
    assert set(sparks_costs()) == ALLOWED_STAR_TIERS
    assert SPARKS_PER_STAR == 12


def test_shop_gifts_keyboard_uses_exchange_rules() -> None:
    from bot.keyboards import shop_gifts_keyboard

    markup = shop_gifts_keyboard()
    labels = {button.text for row in markup.inline_keyboard for button in row}

    assert "100★ · 1 200 искр" in labels
    assert "400★ · 4 800 искр" in labels
    assert "100★ · 1 000 искр" not in labels
