from __future__ import annotations

import re
from typing import Any

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.errors import _is_http_status_message, translate_error
from bot.gender_api import gender_to_api

GENDER_MALE = 1
GENDER_FEMALE = 2
GENDER_BOTH = 3
AGE_ALL = 0
TOP_AGE_GROUPS = (
    (11, 12, 13),
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

def format_sparks_amount(value: Any) -> str:
    try:
        amount = int(value)
    except (TypeError, ValueError):
        return "0"
    return f"{amount:,}".replace(",", " ")


def format_insufficient_sparks_alert(cost: int, balance: int) -> str:
    return (
        "Недостаточно искр для этого подарка.\n\n"
        f"Нужно: {format_sparks_amount(cost)} ✨\n"
        f"У вас: {format_sparks_amount(balance)} ✨"
    )


def _looks_like_technical_gift_error(message: str) -> bool:
    lowered = message.lower()
    if _is_http_status_message(message):
        return True
    technical_markers = (
        "grpc",
        "rpc",
        "unavailable",
        "deadline exceeded",
        "statuscode",
        "connection refused",
        "connection reset",
        "timed out",
        "timeout",
        "internal server error",
        "traceback",
        "exception",
        "failed parsing",
        "goaway",
    )
    return any(marker in lowered for marker in technical_markers)


def format_gift_failure_details(raw_message: str | None) -> str:
    normalized = (raw_message or "").strip()
    mapping = {
        "Недостаточно искр на балансе": "На балансе не хватает искр для этого обмена.",
        "Не удалось списать искры": "На балансе не хватает искр для этого обмена.",
        "Для начала нужно приобрести вип статус": "Сначала оформите VIP в разделе «✨ Привилегии».",
        "Подарок не отправлен. Искры возвращены на баланс.": texts.GIFT_EXCHANGE_UNAVAILABLE,
        "Подарок не отправлен, а откат искр не выполнен. Обратитесь в поддержку.": (
            "Произошла ошибка при обмене. Напишите в поддержку — мы поможем."
        ),
        "Недопустимая стоимость подарка": "Выбран недоступный номинал подарка.",
    }
    if normalized in mapping:
        return mapping[normalized]
    if "не найден" in normalized.lower() and "★" in normalized:
        return "Сейчас нет доступного подарка на эту сумму. Попробуйте другой номинал."
    if not normalized or _looks_like_technical_gift_error(normalized):
        return texts.GIFT_EXCHANGE_UNAVAILABLE
    return normalized


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
        "gender": gender_to_api(gender),
    }


def format_api_error(exc: ApiError) -> str:
    return translate_error(exc.code, exc.message, status=exc.status)


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


def extract_rating(payload: dict[str, Any]) -> float:
    value = payload.get("rating")
    if value is None:
        value = payload.get("Rating", 0)
    try:
        return float(value)
    except (TypeError, ValueError):
        return 0.0


def extract_rating_count(payload: dict[str, Any]) -> int:
    value = payload.get("ratingCount")
    if value is None:
        value = payload.get("RatingCount", 0)
    try:
        return int(value)
    except (TypeError, ValueError):
        return 0


def format_set_photo_saved_text(result: dict[str, Any]) -> str:
    from bot import texts

    city = result.get("city") or result.get("City") or ""
    return texts.PHOTO_SAVED.format(
        city=format_city_display(str(city)),
        rating_line=format_rating_display(
            extract_rating(result),
            extract_rating_count(result),
        ),
    )


def feed_age_group(age: int | None) -> tuple[int, int, int] | None:
    if not isinstance(age, int):
        return None
    for group in TOP_AGE_GROUPS:
        if age in group:
            return group
    return None


def format_feed_age_range(age: int | None) -> str:
    if age == AGE_ALL:
        return "все возраста"
    group = feed_age_group(age)
    if group is None:
        return "категория не определена"
    return f"{group[0]}-{group[2]} лет"


def format_feed_age_value(age: int | None) -> str:
    if age == AGE_ALL:
        return "Все возраста"
    if isinstance(age, int):
        return str(age)
    return "—"


async def main_menu_for(api: LooksRatingApiClient, telegram_id: int):
    from bot.keyboards import main_menu

    return main_menu()


def _user_flag(user: dict | None, *keys: str) -> bool:
    if not isinstance(user, dict):
        return False
    for key in keys:
        value = user.get(key)
        if value is not None:
            return bool(value)
    return False


def _user_field(user: dict | None, *keys: str) -> str | None:
    if not isinstance(user, dict):
        return None
    for key in keys:
        value = user.get(key)
        if value is not None:
            text = str(value).strip()
            if text:
                return text
    return None


def resolve_display_preference_action(user: dict | None) -> str | None:
    has_username = bool(_user_field(user, "telegramUsername", "TelegramUsername"))
    if not has_username:
        return None
    if _user_flag(user, "usesTelegramUsernameAsDisplay", "UsesTelegramUsernameAsDisplay"):
        return "hide"
    return "show"


def build_settings_menu_text(user: dict | None) -> str:
    lines = [texts.SETTINGS_MENU]
    action = resolve_display_preference_action(user)
    if action == "hide":
        lines.append(texts.SETTINGS_MENU_HIDE_USERNAME)
    elif action == "show":
        lines.append(texts.SETTINGS_MENU_SHOW_USERNAME)
    return "\n".join(lines)


async def settings_menu_for(api: LooksRatingApiClient, telegram_id: int, user: dict | None = None):
    from bot.keyboards import settings_keyboard

    if user is None:
        user = await api.get_user(telegram_id)
    photo_payload = await api.get_my_photo(telegram_id) if user else None
    photos = []
    photo_count = 0
    can_add_photo = False
    if isinstance(photo_payload, dict):
        photos = list(photo_payload.get("photos") or [])
        photo_count = int(photo_payload.get("photoCount", len(photos)))
        can_add_photo = bool(photo_payload.get("canAddPhoto", False))
    has_photo = photo_count > 0 or len(photos) > 0
    has_vip = bool(user.get("hasVip")) if isinstance(user, dict) else False
    return settings_keyboard(
        has_photo=has_photo,
        has_vip=has_vip,
        photo_count=photo_count,
        can_add_photo=can_add_photo,
        display_preference_action=resolve_display_preference_action(user),
    )


async def send_settings_menu(
    message,
    api: LooksRatingApiClient,
    telegram_id: int,
    text: str | None = None,
    user: dict | None = None,
) -> None:
    if user is None:
        user = await api.get_user(telegram_id)
    menu_text = text if text is not None else build_settings_menu_text(user)
    await message.answer(menu_text, reply_markup=await settings_menu_for(api, telegram_id, user=user))


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
        age=format_feed_age_value(user.get("age")),
        age_range=format_feed_age_range(user.get("age")),
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


async def load_chapters_catalog(
    api: LooksRatingApiClient,
) -> tuple[list[dict[str, Any]], dict[str, Any] | None]:
    current = await api.get_current_season()
    chapters = await api.get_chapters(include_seasons=True)
    normalized: list[dict[str, Any]] = []
    for chapter in chapters:
        seasons = list(chapter.get("seasons") or [])
        item = dict(chapter)
        item["seasons"] = seasons
        item["seasonsCount"] = len(seasons) if seasons else int(chapter.get("seasonsCount") or 0)
        normalized.append(item)
    return normalized, current


async def load_seasons_for_chapter(
    api: LooksRatingApiClient,
    chapter_id: str,
) -> tuple[list[dict[str, Any]], dict[str, Any] | None]:
    current = await api.get_current_season()
    seasons = await api.get_seasons_by_chapter(chapter_id)
    if seasons:
        return seasons, current

    chapters = await api.get_chapters(include_seasons=True)
    chapter = next((item for item in chapters if str(item.get("id")) == str(chapter_id)), None)
    if chapter:
        embedded = list(chapter.get("seasons") or [])
        if embedded:
            return embedded, current

    if current and str(current.get("listSeasonsId")) == str(chapter_id):
        return [current], current

    return seasons, current
