"""Age bracket rules mirrored from LooksRatingApi.Services.TopService."""

from __future__ import annotations

AGE_ALL = 0
MIN_BRACKET_AGE = 14
MAX_BRACKET_AGE = 46
AGE_BRACKET_INPUT_LABEL = f"{MIN_BRACKET_AGE}–{MAX_BRACKET_AGE}"

TOP_AGE_GROUPS: tuple[tuple[int, int, int], ...] = (
    (14, 15, 16),
    (17, 18, 19),
    (20, 21, 22),
    (23, 24, 25),
    (26, 27, 28),
    (28, 30, 31),
    (32, 33, 34),
    (35, 36, 37),
    (38, 39, 40),
    (41, 42, 43),
    (44, 45, 46),
)


def feed_age_group(age: int | None) -> tuple[int, int, int] | None:
    if not isinstance(age, int):
        return None
    for group in TOP_AGE_GROUPS:
        if age in group:
            return group
    return None


def is_valid_bracket_age(age: int) -> bool:
    return feed_age_group(age) is not None


def is_valid_feed_age(age: int) -> bool:
    return age == AGE_ALL or is_valid_bracket_age(age)


def is_valid_nomination_age(age: int) -> bool:
    return is_valid_bracket_age(age)


def parse_feed_age_text(text: str, *, all_ages_button: str) -> int | None:
    stripped = text.strip()
    if stripped == all_ages_button:
        return AGE_ALL
    try:
        age = int(stripped)
    except ValueError:
        return None
    return age if is_valid_feed_age(age) else None


def parse_nomination_age_text(text: str) -> int | None:
    try:
        age = int(text.strip())
    except ValueError:
        return None
    return age if is_valid_nomination_age(age) else None
