from __future__ import annotations

import re
from typing import Any

from api.client import ApiError, LooksRatingApiClient
from bot.errors import translate_error

GENDER_MALE = 1
GENDER_FEMALE = 2
GENDER_BOTH = 3

class SessionState:
    START = "Start"
    AWAITING_DISPLAY_NAME = "AwaitingDisplayName"
    REGISTERED = "Registered"
    AWAITING_PHOTO = "AwaitingPhoto"
    AWAITING_FEED_CITY = "AwaitingFeedCity"
    AWAITING_FEED_AGE = "AwaitingFeedAge"
    AWAITING_FEED_GENDER = "AwaitingFeedGender"
    RATING = "Rating"
    IDLE = "Idle"


async def ensure_bot_session(api: LooksRatingApiClient, telegram_id: int) -> dict[str, Any]:
    return await api.ensure_session(telegram_id, SessionState.START)


async def set_bot_state(api: LooksRatingApiClient, telegram_id: int, state: str) -> None:
    await api.update_session_state(telegram_id, state)


def parse_gender_value(value: Any) -> int:
    if value is None:
        return 0
    if isinstance(value, int):
        return value
    if isinstance(value, str):
        normalized = value.strip()
        if normalized.isdigit():
            return int(normalized)
        mapping = {
            "Male": GENDER_MALE,
            "Female": GENDER_FEMALE,
            "MaleFamale": GENDER_BOTH,
            "Unknown": 0,
        }
        return mapping.get(normalized, 0)
    return 0


def gender_label(value: Any) -> str:
    gender = parse_gender_value(value)
    if gender == GENDER_MALE:
        return "Мужской"
    if gender == GENDER_FEMALE:
        return "Женский"
    if gender == GENDER_BOTH:
        return "Оба"
    return "Не указан"


def feed_gender_from_text(text: str) -> int | None:
    normalized = text.strip().lower()
    if any(token in normalized for token in ("мужск", "male", "👨")) or normalized in {"м", "1"}:
        return GENDER_MALE
    if any(token in normalized for token in ("женск", "female", "👩")) or normalized in {"ж", "2"}:
        return GENDER_FEMALE
    if any(token in normalized for token in ("оба", "both", "👥")) or normalized in {"3"}:
        return GENDER_BOTH
    return None


def gender_from_text(text: str) -> int | None:
    normalized = text.strip().lower()
    if any(token in normalized for token in ("мужск", "male", "👨")) or normalized in {"м", "1"}:
        return GENDER_MALE
    if any(token in normalized for token in ("женск", "female", "👩")) or normalized in {"ж", "2"}:
        return GENDER_FEMALE
    return None


def profile_nomination() -> dict[str, Any]:
    return {"useProfileNomination": True}


def custom_nomination(city: str, age: int, gender: int) -> dict[str, Any]:
    return {
        "useProfileNomination": False,
        "city": city.strip().lower(),
        "age": age,
        "gender": gender,
    }


def format_api_error(exc: ApiError) -> str:
    return translate_error(exc.code, exc.message)


async def load_cities(api: LooksRatingApiClient) -> list[str]:
    cities = await api.get_cities()
    return sorted(set(c.strip().lower() for c in cities if c.strip()))


def normalize_city_input(text: str) -> str:
    value = text.strip().lower()
    value = re.sub(r"^г\.\s*", "", value)
    value = re.sub(r"\s+", " ", value)
    return value


def resolve_city_name(user_input: str, cities: list[str]) -> str | None:
    normalized = normalize_city_input(user_input)
    if not normalized:
        return None

    city_set = set(cities)
    if normalized in city_set:
        return normalized

    variants = {
        normalized.replace(" ", "-"),
        normalized.replace("-", " "),
        normalized.replace("ё", "е"),
    }
    for variant in variants:
        if variant in city_set:
            return variant

    for city in cities:
        if city.replace("-", " ") == normalized.replace("-", " "):
            return city

    return None


def format_city_display(city: str | None) -> str:
    if not city or city.strip() in {"", "—"}:
        return "—"
    return city.strip().title()


def format_rating_display(rating: float, count: int) -> str:
    return f"{rating:.1f}/10 · {count} оценок"


async def main_menu_for(api: LooksRatingApiClient, telegram_id: int):
    from bot.keyboards import main_menu

    return main_menu()


async def settings_menu_for(api: LooksRatingApiClient, telegram_id: int):
    from bot.keyboards import settings_keyboard

    user = await api.get_user(telegram_id)
    has_photo = bool(user and user.get("hasPhoto"))
    return settings_keyboard(has_photo=has_photo)


async def send_settings_menu(message, api: LooksRatingApiClient, telegram_id: int, text: str) -> None:
    await message.answer(text, reply_markup=await settings_menu_for(api, telegram_id))


async def send_main_menu(
    message,
    api: LooksRatingApiClient,
    telegram_id: int,
    text: str,
) -> None:
    await message.answer(text, reply_markup=await main_menu_for(api, telegram_id))


def format_feed_view(user: dict[str, Any]) -> str:
    from bot import texts

    return texts.FEED_VIEW.format(
        city=format_city_display(user.get("city")),
        age=user.get("age", "—"),
        gender=gender_label(user.get("gender")),
    )


async def send_feed_view(
    message,
    api: LooksRatingApiClient,
    telegram_id: int,
    *,
    prefix: str = "",
    include_edit_hint: bool = True,
) -> None:
    from bot import texts
    from bot.keyboards import profile_edit_keyboard

    user = await api.get_user(telegram_id)
    if not user or not user.get("hasRecommendationSettings"):
        await message.answer(texts.NEED_START)
        return

    body = format_feed_view(user)
    if include_edit_hint:
        body += texts.FEED_EDIT_HINT
    text = f"{prefix}{body}" if prefix else body
    await message.answer(text, reply_markup=profile_edit_keyboard())


async def load_seasons_catalog(
    api: LooksRatingApiClient,
) -> tuple[list[dict[str, Any]], dict[str, Any] | None]:
    current = await api.get_current_season()
    chapter = await api.get_latest_chapter()
    seasons = list((chapter or {}).get("seasons") or [])
    if seasons:
        return seasons, current

    chapters = await api.get_chapters(include_seasons=True)
    for item in chapters:
        chapter_seasons = item.get("seasons") or []
        if chapter_seasons:
            return list(chapter_seasons), current

    if current:
        return [current], current
    return [], current
