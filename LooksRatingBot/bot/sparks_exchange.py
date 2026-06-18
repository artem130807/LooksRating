"""Canonical sparks-to-Telegram-Stars exchange rules for the VIP gift shop.

Must stay in sync with LooksRatingApi.Services.SparksWallet.SparksGiftExchangeRules.
"""

from __future__ import annotations

SPARKS_PER_STAR = 12

ALLOWED_STAR_TIERS: frozenset[int] = frozenset({100, 200, 300, 400})


def is_allowed_star_tier(star_tier: int) -> bool:
    return star_tier in ALLOWED_STAR_TIERS


def sparks_cost(star_tier: int) -> int | None:
    if not is_allowed_star_tier(star_tier):
        return None
    return star_tier * SPARKS_PER_STAR


def sparks_costs() -> dict[int, int]:
    return {tier: tier * SPARKS_PER_STAR for tier in sorted(ALLOWED_STAR_TIERS)}
