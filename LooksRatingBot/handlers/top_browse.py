from __future__ import annotations

from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, InlineKeyboardButton, InlineKeyboardMarkup, Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import MENU_TOP, top_gender_pick_keyboard, tops_menu_keyboard
from bot.services import (
    AGE_ALL,
    format_api_error,
    format_city_display,
    format_feed_age_range,
    format_rating_display,
    load_seasons_catalog,
    send_main_menu,
)

router = Router()

PAGE_SIZE = 10


def _top_user_rows(items: list[dict]) -> list[list[InlineKeyboardButton]]:
    rows: list[list[InlineKeyboardButton]] = []
    for item in items[:10]:
        photo_id = item.get("id")
        if not photo_id:
            continue
        rows.append(
            [
                InlineKeyboardButton(
                    text=f"{item.get('place', '?')}. {item.get('name', 'Участник')}",
                    callback_data=f"top:user:{photo_id}",
                )
            ]
        )
    return rows


def _top_nav_keyboard(
    season_id: str,
    page: int,
    total_pages: int,
    gender: int,
    items: list[dict],
) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    rows.extend(_top_user_rows(items))
    nav: list[InlineKeyboardButton] = []
    if page > 1:
        nav.append(
            InlineKeyboardButton(text="◀️ Назад", callback_data=f"top:open:season:{season_id}:{page - 1}:{gender}")
        )
    if total_pages > 0:
        nav.append(
            InlineKeyboardButton(
                text=f"{page}/{max(total_pages, 1)}",
                callback_data=f"top:open:season:{season_id}:{page}:{gender}",
            )
        )
    if page < total_pages:
        nav.append(
            InlineKeyboardButton(text="Вперёд ▶️", callback_data=f"top:open:season:{season_id}:{page + 1}:{gender}")
        )
    if nav:
        rows.append(nav)
    rows.append(
        [
            InlineKeyboardButton(text="📸 Моё фото в сезоне", callback_data=f"top:my:{season_id}"),
            InlineKeyboardButton(text="📅 Сезоны", callback_data="season:list"),
        ]
    )
    rows.append([InlineKeyboardButton(text="📱 В меню", callback_data="top:menu")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def _top_weekly_keyboard(items: list[dict]) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    rows.extend(_top_user_rows(items))
    rows.append([InlineKeyboardButton(text="📅 Топы по сезонам", callback_data="season:list")])
    rows.append([InlineKeyboardButton(text="📱 В меню", callback_data="top:menu")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def _back_to_top_keyboard(scope: str, *, season_id: str | None, page: int, gender: int) -> InlineKeyboardMarkup:
    if scope == "season" and season_id:
        callback_data = f"top:open:season:{season_id}:{page}:{gender}"
    else:
        callback_data = f"top:open:weekly:{gender}"
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text=texts.TOP_BACK_TO_LIST, callback_data=callback_data)]
        ]
    )


def _format_top_message(data: dict) -> str:
    current = texts.TOP_CURRENT if data.get("isCurrentSeason") else ""
    closed = texts.TOP_CLOSED if data.get("isClosed") else ""
    header = texts.TOP_HEADER.format(
        season_name=data.get("seasonName", "Сезон"),
        season_number=data.get("seasonNumber", ""),
        current=current,
        closed=closed,
        page=data.get("page", 1),
        total_pages=max(data.get("totalPages", 0), 1),
        total=data.get("totalCount", 0),
    )
    items = data.get("items", [])
    if not items:
        return header + "\n" + texts.TOP_EMPTY
    lines = [header]
    for item in items:
        lines.append(
            texts.TOP_LINE.format(
                place=item.get("place", "?"),
                name=item.get("name", "—"),
                rating_line=format_rating_display(
                    float(item.get("rating", 0)),
                    int(item.get("ratingCount", 0)),
                ),
                gender=item.get("genderNomination", ""),
                age=item.get("ageNomination", ""),
            )
        )
    return "\n".join(lines)


def _format_top_filter(scope: str, gender: int, age: int) -> str:
    scope_label = texts.TOP_SCOPE_WEEKLY if scope == "weekly" else texts.TOP_SCOPE_SEASON
    gender_label = texts.TOP_GENDER_MALE if gender == 1 else texts.TOP_GENDER_FEMALE
    return texts.TOP_SELECTED_FILTER.format(
        scope=scope_label,
        gender=gender_label,
        age_range=format_feed_age_range(age),
    )


def _format_weekly_now_message(items: list[dict], *, gender: int, age: int) -> str:
    filter_line = _format_top_filter("weekly", gender, age)
    if not items:
        return texts.TOP_WEEKLY_HEADER + filter_line + "\n\n" + texts.TOP_WEEKLY_EMPTY

    lines = [texts.TOP_WEEKLY_HEADER + filter_line]
    for item in items:
        lines.append(
            texts.TOP_LINE.format(
                place=item.get("place", "?"),
                name=item.get("name", "—"),
                rating_line=format_rating_display(
                    float(item.get("rating", 0)),
                    int(item.get("ratingCount", 0)),
                ),
                gender=item.get("genderNomination", ""),
                age=item.get("ageNomination", ""),
            )
        )
    return "\n".join(lines)


async def show_top_page(
    target: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
    *,
    gender: int,
    season_id: str | None,
    page: int,
    edit: bool = False,
) -> None:
    user = await api.get_user(telegram_id)
    if not user:
        await target.answer(texts.NEED_START)
        return
    if not user.get("hasRecommendationSettings"):
        await target.answer(texts.TOP_SETUP_REQUIRED)
        return
    age = user.get("age")
    if not isinstance(age, int) or age < AGE_ALL or age > 100:
        await target.answer(texts.TOP_AGE_REQUIRED)
        return
    if gender not in {1, 2}:
        await target.answer(texts.TOP_GENDER_PICK_REQUIRED)
        return
    try:
        data = await api.get_top_photos(
            telegram_id,
            gender,
            age,
            season_id=season_id,
            page=page,
            page_size=PAGE_SIZE,
        )
    except ApiError as exc:
        await target.answer(format_api_error(exc))
        return

    sid = str(data.get("seasonId", season_id or ""))
    total_pages = int(data.get("totalPages", 0))
    page_num = int(data.get("page", page))
    items = data.get("items", [])
    text = _format_top_filter("season", gender, age) + "\n\n" + _format_top_message(data)
    markup = _top_nav_keyboard(sid, page_num, total_pages, gender, items)
    if edit:
        try:
            await target.edit_text(text, reply_markup=markup)
            return
        except Exception:
            pass
    await target.answer(text, reply_markup=markup)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_TOP)
async def menu_top(message: Message, api: LooksRatingApiClient) -> None:
    await message.answer(texts.TOPS_MENU, reply_markup=tops_menu_keyboard())


async def _send_gender_pick(
    target: Message,
    *,
    age: int,
    scope: str,
    season_id: str | None = None,
    edit: bool = False,
) -> None:
    text = texts.TOP_GENDER_PICK.format(age_range=format_feed_age_range(age))
    markup = top_gender_pick_keyboard(scope, season_id)
    if edit:
        try:
            await target.edit_text(text, reply_markup=markup)
            return
        except Exception:
            pass
    await target.answer(text, reply_markup=markup)


@router.callback_query(F.data == "top:weekly:pick")
async def top_weekly_pick(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    settings = await api.get_recommendation_settings(callback.from_user.id)
    if not settings:
        await callback.answer(
            texts.TOP_SETUP_REQUIRED,
            show_alert=True,
        )
        return

    age = settings.get("age")
    if not isinstance(age, int) or age < AGE_ALL or age > 100:
        await callback.answer(
            texts.TOP_AGE_REQUIRED,
            show_alert=True,
        )
        return

    if callback.message:
        await _send_gender_pick(callback.message, age=age, scope="weekly", edit=True)
    await callback.answer()


@router.callback_query(F.data.startswith("top:pick:"))
async def top_season_pick(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    season_id = callback.data.removeprefix("top:pick:")
    user = await api.get_user(callback.from_user.id)
    if not user or not user.get("hasRecommendationSettings"):
        await callback.answer(
            texts.TOP_SETUP_REQUIRED,
            show_alert=True,
        )
        return

    age = user.get("age")
    if not isinstance(age, int) or age < AGE_ALL or age > 100:
        await callback.answer(
            texts.TOP_AGE_REQUIRED,
            show_alert=True,
        )
        return

    if callback.message:
        await _send_gender_pick(callback.message, age=age, scope="season", season_id=season_id, edit=True)
    await callback.answer()


@router.callback_query(F.data.startswith("top:open:weekly:"))
async def top_weekly_now(callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient) -> None:
    gender_str = callback.data.removeprefix("top:open:weekly:")
    try:
        gender = int(gender_str)
    except ValueError:
        await callback.answer(texts.TOP_PICK_ERROR, show_alert=True)
        return

    settings = await api.get_recommendation_settings(callback.from_user.id)
    if not settings:
        await callback.answer(texts.TOP_SETUP_REQUIRED, show_alert=True)
        return
    age = settings.get("age")
    if not isinstance(age, int) or age < AGE_ALL or age > 100:
        await callback.answer(texts.TOP_AGE_REQUIRED, show_alert=True)
        return

    try:
        items = await api.get_the_best_week_photos_now(callback.from_user.id, gender, age)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return

    if callback.message:
        text = _format_weekly_now_message(items, gender=gender, age=age)
        markup = _top_weekly_keyboard(items)
        await state.update_data(
            top_context={
                "scope": "weekly",
                "season_id": None,
                "page": 1,
                "gender": gender,
            }
        )
        try:
            await callback.message.edit_text(text, reply_markup=markup)
        except Exception:
            await callback.message.answer(text, reply_markup=markup)
    await callback.answer()


@router.callback_query(F.data.startswith("top:open:season:"))
async def top_page_callback(callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient) -> None:
    payload = callback.data.removeprefix("top:open:season:")
    try:
        season_id, page_str, gender_str = payload.rsplit(":", 2)
        page = int(page_str)
        gender = int(gender_str)
    except ValueError:
        await callback.answer(texts.TOP_PICK_ERROR, show_alert=True)
        return
    if callback.message:
        await state.update_data(
            top_context={
                "scope": "season",
                "season_id": season_id,
                "page": page,
                "gender": gender,
            }
        )
        await show_top_page(
            callback.message,
            api,
            callback.from_user.id,
            gender=gender,
            season_id=season_id,
            page=page,
            edit=True,
        )
    await callback.answer()


@router.callback_query(F.data == "top:seasons")
async def top_seasons_list(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    seasons, current = await load_seasons_catalog(api)
    if not seasons:
        await callback.answer(texts.SEASON_NOT_FOUND, show_alert=True)
        return
    current_id = str(current["id"]) if current else None
    from bot.keyboards import seasons_list_keyboard

    markup = seasons_list_keyboard(seasons, current_id)
    if callback.message:
        try:
            await callback.message.edit_text(texts.SEASONS_LIST, reply_markup=markup)
        except Exception:
            await callback.message.answer(texts.SEASONS_LIST, reply_markup=markup)
    await callback.answer()


@router.callback_query(F.data.startswith("top:my:"))
async def top_my_photo_season(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    season_id = callback.data.removeprefix("top:my:")
    photo = await api.get_my_photo_by_season(callback.from_user.id, season_id)
    if not photo:
        await callback.answer(texts.TOP_MY_PHOTO_MISSING, show_alert=True)
        return
    chapter = await api.get_latest_chapter()
    season_name = "сезон"
    if chapter and chapter.get("seasons"):
        for s in chapter["seasons"]:
            if str(s.get("id")) == season_id:
                season_name = f"{s.get('name', '')} №{s.get('number', '')}"
                break
    caption = texts.TOP_MY_PHOTO_SEASON.format(
        season_name=season_name,
        rating_line=format_rating_display(
            float(photo.get("rating", 0)),
            int(photo.get("ratingCount", 0)),
        ),
        rank=photo.get("rank", "—"),
        city=format_city_display(photo.get("city")),
        age=photo.get("age", "—"),
        gender=photo.get("gender", "—"),
    )
    if callback.message:
        await callback.message.answer_photo(photo["telegramFileId"], caption=caption)
    await callback.answer()


@router.callback_query(F.data.startswith("top:user:"))
async def top_user_profile(callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient) -> None:
    photo_id = callback.data.removeprefix("top:user:")
    try:
        profile = await api.get_photo_user_by_id(photo_id)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return

    if not profile:
        await callback.answer("Профиль участника не найден.", show_alert=True)
        return

    caption = texts.TOP_USER_PROFILE.format(
        name=profile.get("userName", "Участник"),
        gender=profile.get("gender", "—"),
        age=profile.get("age", "—"),
        city=format_city_display(profile.get("city")),
        rank=profile.get("rank", "—"),
        rating_line=format_rating_display(
            float(profile.get("rating", 0)),
            int(profile.get("ratingCount", 0)),
        ),
    )
    state_data = await state.get_data()
    context = state_data.get("top_context") or {}
    back_markup = _back_to_top_keyboard(
        context.get("scope", "weekly"),
        season_id=context.get("season_id"),
        page=int(context.get("page", 1)),
        gender=int(context.get("gender", 1)),
    )
    if callback.message and profile.get("image"):
        await callback.message.answer_photo(profile.get("image"), caption=caption, reply_markup=back_markup)
    elif callback.message:
        await callback.message.answer(caption, reply_markup=back_markup)
    await callback.answer()


@router.callback_query(F.data == "top:menu")
async def top_back_menu(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await send_main_menu(callback.message, api, callback.from_user.id, texts.MAIN_MENU)
    await callback.answer()
