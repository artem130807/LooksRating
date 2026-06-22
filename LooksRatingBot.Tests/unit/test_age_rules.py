import pytest

from bot.age_rules import (
    AGE_ALL,
    AGE_BRACKET_INPUT_LABEL,
    MAX_BRACKET_AGE,
    MIN_BRACKET_AGE,
    feed_age_group,
    is_valid_bracket_age,
    is_valid_feed_age,
    is_valid_nomination_age,
    parse_feed_age_text,
    parse_nomination_age_text,
)


@pytest.mark.parametrize(
    "age",
    [11, 13, 47, 67, 100],
)
def test_unsupported_ages_have_no_bracket(age: int) -> None:
    assert feed_age_group(age) is None
    assert is_valid_bracket_age(age) is False


@pytest.mark.parametrize("age", [14, 16, 46])
def test_supported_ages_have_bracket(age: int) -> None:
    assert feed_age_group(age) is not None
    assert is_valid_bracket_age(age) is True


@pytest.mark.parametrize(
    ("age", "expected"),
    [
        (AGE_ALL, True),
        (14, True),
        (46, True),
        (67, False),
        (11, False),
    ],
)
def test_is_valid_feed_age(age: int, expected: bool) -> None:
    assert is_valid_feed_age(age) is expected


def test_parse_feed_age_text_accepts_all_ages_button() -> None:
    assert parse_feed_age_text("🌐 Все возраста", all_ages_button="🌐 Все возраста") == AGE_ALL


def test_parse_feed_age_text_rejects_out_of_range() -> None:
    assert parse_feed_age_text("67", all_ages_button="🌐 Все возраста") is None


def test_parse_nomination_age_text_accepts_bracket_age() -> None:
    assert parse_nomination_age_text("20") == 20


def test_parse_nomination_age_text_rejects_all_ages() -> None:
    assert parse_nomination_age_text("0") is None


def test_bracket_bounds_match_api() -> None:
    assert MIN_BRACKET_AGE == 14
    assert MAX_BRACKET_AGE == 46
    assert AGE_BRACKET_INPUT_LABEL == "14–46"
