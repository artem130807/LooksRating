from __future__ import annotations

from aiogram import F, Router
from aiogram.types import CallbackQuery, InlineKeyboardButton, InlineKeyboardMarkup, Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import MENU_TOP, tops_menu_keyboard
from bot.services import format_api_error, format_city_display, format_rating_display, load_seasons_catalog, parse_gender_value, send_main_menu

router = Router()

PAGE_SIZE = 10


def _top_nav_keyboard(season_id: str, page: int, total_pages: int) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    nav: list[InlineKeyboardButton] = []
    if page > 1:
        nav.append(
            InlineKeyboardButton(text="◀️ Назад", callback_data=f"top:p:{season_id}:{page - 1}")
        )
    if total_pages > 0:
        nav.append(
            InlineKeyboardButton(
                text=f"{page}/{max(total_pages, 1)}",
                callback_data=f"top:p:{season_id}:{page}",
            )
        )
    if page < total_pages:
        nav.append(
            InlineKeyboardButton(text="Вперёд ▶️", callback_data=f"top:p:{season_id}:{page + 1}")
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


def _format_weekly_now_message(items: list[dict]) -> str:
    if not items:
        return texts.TOP_WEEKLY_HEADER + "\n" + texts.TOP_WEEKLY_EMPTY

    lines = [texts.TOP_WEEKLY_HEADER]
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
    season_id: str | None,
    page: int,
    edit: bool = False,
) -> None:
    user = await api.get_user(telegram_id)
    if not user:
        await target.answer(texts.NEED_START)
        return
    if not user.get("hasRecommendationSettings"):
        await target.answer(
            "Сначала настройте ленту: нажмите «⭐ Оценить» и укажите город, возраст и пол."
        )
        return
    age = user.get("age")
    gender = parse_gender_value(user.get("gender"))
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
    text = _format_top_message(data)
    markup = _top_nav_keyboard(sid, page_num, total_pages)
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


@router.callback_query(F.data == "top:weekly:now")
async def top_weekly_now(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    settings = await api.get_recommendation_settings(callback.from_user.id)
    if not settings:
        await callback.answer(
            "Сначала настройте ленту через «⭐ Оценить».",
            show_alert=True,
        )
        return

    age = settings.get("age")
    gender = parse_gender_value(settings.get("gender"))
    if not isinstance(age, int) or age < 14 or age > 100 or gender <= 0:
        await callback.answer(
            "Сначала обновите настройки ленты через «⭐ Оценить».",
            show_alert=True,
        )
        return

    try:
        items = await api.get_the_best_week_photos_now(callback.from_user.id, gender, age)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return

    if callback.message:
        text = _format_weekly_now_message(items)
        try:
            await callback.message.edit_text(text, reply_markup=tops_menu_keyboard())
        except Exception:
            await callback.message.answer(text, reply_markup=tops_menu_keyboard())
    await callback.answer()


@router.callback_query(F.data.startswith("top:p:"))
async def top_page_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    payload = callback.data.removeprefix("top:p:")
    season_id, page_str = payload.rsplit(":", 1)
    page = int(page_str)
    if callback.message:
        await show_top_page(
            callback.message,
            api,
            callback.from_user.id,
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


@router.callback_query(F.data == "top:menu")
async def top_back_menu(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await send_main_menu(callback.message, api, callback.from_user.id, texts.MAIN_MENU)
    await callback.answer()
