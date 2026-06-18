from __future__ import annotations

import pytest

from api.client import ApiError
from bot.errors import translate_error
from bot.services import (
    SessionState,
    custom_nomination,
    format_api_error,
    format_city_display,
    format_gift_failure_details,
    format_insufficient_sparks_alert,
    format_sparks_amount,
    gender_from_text,
    gender_label,
    normalize_city_input,
    parse_gender_value,
    resolve_city_name,
)
from helpers.fakes import FakeApiClient


class TestCityResolution:
    def test_resolve_exact_match(self) -> None:
        cities = ["moscow", "saint-petersburg"]

        assert resolve_city_name("Moscow", cities) == "moscow"

    def test_resolve_hyphen_and_space_variants(self) -> None:
        cities = ["saint-petersburg", "nizhny novgorod"]

        assert resolve_city_name("Saint Petersburg", cities) == "saint-petersburg"
        assert resolve_city_name("Nizhny-Novgorod", cities) == "nizhny novgorod"

    def test_resolve_yo_to_e(self) -> None:
        cities = ["ярославль"]

        assert resolve_city_name("Ярославль", cities) == "ярославль"

    def test_resolve_unknown_city_returns_none(self) -> None:
        assert resolve_city_name("Unknown City", ["moscow"]) is None

    def test_normalize_city_input_strips_prefix(self) -> None:
        assert normalize_city_input("г. Москва") == "москва"


class TestGenderAndNomination:
    def test_gender_from_text_recognizes_russian_and_emoji(self) -> None:
        assert gender_from_text("Мужской") == 1
        assert gender_from_text("женский") == 2
        assert gender_from_text("👨") == 1

    def test_gender_from_text_rejects_both_for_photo_nomination(self) -> None:
        assert gender_from_text("Оба") is None

    def test_custom_nomination_payload(self) -> None:
        nomination = custom_nomination("Moscow", 25, 1)

        assert nomination == {
            "useProfileNomination": False,
            "city": "moscow",
            "age": 25,
            "gender": "Male",
        }

    @pytest.mark.parametrize(
        ("raw", "label"),
        [
            (1, "Мужской"),
            ("Female", "Женский"),
            ("MaleFamale", "Оба"),
        ],
    )
    def test_gender_label(self, raw: object, label: str) -> None:
        assert gender_label(raw) == label

    def test_parse_gender_value_from_api_strings(self) -> None:
        assert parse_gender_value("Male") == 1
        assert parse_gender_value("Female") == 2


class TestFormatting:
    def test_format_city_display_title_case(self) -> None:
        assert format_city_display("moscow") == "Moscow"
        assert format_city_display("") == "—"

    def test_format_sparks_amount_uses_spaces(self) -> None:
        assert format_sparks_amount(4000) == "4 000"

    def test_format_insufficient_sparks_alert(self) -> None:
        text = format_insufficient_sparks_alert(500, 100)

        assert "500" in text
        assert "100" in text


class TestApiErrors:
    def test_translate_known_error_code(self) -> None:
        assert "уже есть фото" in translate_error("PhotoAlreadyExists").lower()

    def test_format_api_error_uses_translation(self) -> None:
        exc = ApiError(400, code="InvalidNominationAge", message="InvalidNominationAge")

        assert "14" in format_api_error(exc)

    def test_format_api_error_hides_http_500(self) -> None:
        exc = ApiError(500, message="HTTP 500")

        text = format_api_error(exc)

        assert "HTTP" not in text
        assert "500" not in text
        assert "временно" in text.lower()

    def test_translate_unknown_error_falls_back(self) -> None:
        assert translate_error(None, "custom") == "custom"
        assert translate_error(None) == "Произошла ошибка. Попробуйте ещё раз."
        assert "временно" in translate_error(None, "HTTP 500").lower()


class TestGiftFailureDetails:
    def test_format_gift_failure_hides_grpc_error(self) -> None:
        details = format_gift_failure_details(
            '<_InactiveRpcError of RPC that terminated with: status = StatusCode.UNAVAILABLE>'
        )

        assert "grpc" not in details.lower()
        assert "rpc" not in details.lower()
        assert "временно" in details.lower()
        assert "stars" in details.lower()

    def test_format_gift_failure_keeps_insufficient_sparks(self) -> None:
        details = format_gift_failure_details("Недостаточно искр на балансе")

        assert "искр" in details.lower()

    def test_format_gift_failure_empty_message(self) -> None:
        details = format_gift_failure_details(None)

        assert "временно" in details.lower()


@pytest.mark.asyncio
class TestLoadCities:
    async def test_load_cities_sorts_and_deduplicates(self) -> None:
        from bot.services import load_cities

        api = FakeApiClient(cities=["spb", "moscow", "spb", " Moscow "])

        cities = await load_cities(api)

        assert cities == ["moscow", "spb"]
