from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from api.client import LooksRatingApiClient
from bot import texts
from bot.keyboards import chapters_list_keyboard, season_actions_keyboard, seasons_list_keyboard
from bot.services import load_chapters_catalog, load_seasons_for_chapter, send_main_menu

router = Router()


def _season_card_text(season: dict, *, current_id: str | None) -> str:
    chapter_line = ""
    closed_line = texts.SEASON_CLOSED if season.get("isClosed") else ""
    current_line = texts.SEASON_CURRENT if current_id and str(season.get("id")) == current_id else ""
    return texts.SEASON.format(
        name=season.get("name", "Сезон"),
        number=season.get("number", ""),
        count=season.get("photoProfilesCount", season.get("photoUsersCount", 0)),
        chapter=chapter_line,
        closed=closed_line + current_line,
    )


async def send_seasons_list(
    target: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
    chapter_id: str,
    state: FSMContext | None = None,
    *,
    edit: bool = False,
) -> None:
    seasons, current = await load_seasons_for_chapter(api, chapter_id)
    if not seasons:
        await send_main_menu(target, api, telegram_id, texts.SEASON_NOT_FOUND)
        return

    current_id = str(current["id"]) if current else None
    markup = seasons_list_keyboard(seasons, current_id)
    if state is not None:
        season_to_chapter = {str(item.get("id")): chapter_id for item in seasons if item.get("id")}
        await state.update_data(
            last_chapter_id=chapter_id,
            season_chapter_map=season_to_chapter,
        )
    if edit:
        try:
            await target.edit_text(texts.SEASONS_LIST, reply_markup=markup)
            return
        except Exception:
            pass
    await target.answer(texts.SEASONS_LIST, reply_markup=markup)


async def send_chapters_list(
    target: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
    *,
    edit: bool = False,
) -> None:
    chapters, current = await load_chapters_catalog(api)
    if not chapters:
        await send_main_menu(target, api, telegram_id, texts.SEASON_NOT_FOUND)
        return

    chapters = sorted(chapters, key=lambda item: item.get("createdDate", ""), reverse=True)
    current_chapter_id = str(current["listSeasonsId"]) if current and current.get("listSeasonsId") else None
    markup = chapters_list_keyboard(chapters, current_chapter_id)
    if edit:
        try:
            await target.edit_text(texts.CHAPTERS_LIST, reply_markup=markup)
            return
        except Exception:
            pass
    await target.answer(texts.CHAPTERS_LIST, reply_markup=markup)


@router.callback_query(F.data == "chapter:list")
async def chapters_list_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await send_chapters_list(
            callback.message,
            api,
            callback.from_user.id,
            edit=True,
        )
    await callback.answer()


@router.callback_query(F.data.startswith("chapter:open:"))
async def chapter_open_callback(callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient) -> None:
    chapter_id = callback.data.removeprefix("chapter:open:")
    if callback.message:
        await send_seasons_list(
            callback.message,
            api,
            callback.from_user.id,
            chapter_id,
            state,
            edit=True,
        )
    await callback.answer()


@router.callback_query(F.data == "chapter:back")
async def chapter_back_callback(callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient) -> None:
    data = await state.get_data()
    chapter_id = data.get("last_chapter_id")
    if not callback.message:
        await callback.answer()
        return
    if not chapter_id:
        await send_chapters_list(callback.message, api, callback.from_user.id, edit=True)
        await callback.answer()
        return
    await send_seasons_list(callback.message, api, callback.from_user.id, chapter_id, state, edit=True)
    await callback.answer()


@router.callback_query(F.data.startswith("season:open:"))
async def season_open_callback(
    callback: CallbackQuery,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    payload = callback.data.removeprefix("season:open:")
    chapter_id: str | None = None
    season_id = payload
    state_data = await state.get_data()
    chapter_map = state_data.get("season_chapter_map") if isinstance(state_data.get("season_chapter_map"), dict) else {}
    mapped_chapter_id = chapter_map.get(season_id)
    if isinstance(mapped_chapter_id, str) and mapped_chapter_id:
        chapter_id = mapped_chapter_id
    else:
        fallback_chapter_id = state_data.get("last_chapter_id")
        if isinstance(fallback_chapter_id, str) and fallback_chapter_id:
            chapter_id = fallback_chapter_id

    if not chapter_id:
        if callback.message:
            await send_chapters_list(
                callback.message,
                api,
                callback.from_user.id,
                edit=True,
            )
        await callback.answer(texts.SEASON_LIST_REFRESH_REQUIRED, show_alert=True)
        return

    chapters, current = await load_chapters_catalog(api)
    season = None
    if chapter_id:
        chapter_seasons, _ = await load_seasons_for_chapter(api, chapter_id)
        season = next((item for item in chapter_seasons if str(item.get("id")) == season_id), None)
    else:
        for chapter in chapters:
            resolved_chapter_id = str(chapter.get("id"))
            if not resolved_chapter_id:
                continue
            chapter_seasons = list(chapter.get("seasons") or [])
            if not chapter_seasons:
                chapter_seasons, _ = await load_seasons_for_chapter(api, resolved_chapter_id)
            season = next((item for item in chapter_seasons if str(item.get("id")) == season_id), None)
            if season:
                chapter_id = resolved_chapter_id
                break

    if not season:
        if callback.message:
            await send_chapters_list(
                callback.message,
                api,
                callback.from_user.id,
                edit=True,
            )
        await callback.answer(texts.SEASON_LIST_REFRESH_REQUIRED, show_alert=True)
        return

    current_id = str(current["id"]) if current else None
    text = _season_card_text(season, current_id=current_id)
    markup = season_actions_keyboard(season_id)
    if callback.message:
        if chapter_id:
            await state.update_data(last_chapter_id=chapter_id)
        try:
            await callback.message.edit_text(text, reply_markup=markup)
        except Exception:
            await callback.message.answer(text, reply_markup=markup)
    await callback.answer()


@router.callback_query(F.data == "season:menu")
async def season_menu_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await send_main_menu(callback.message, api, callback.from_user.id, texts.MAIN_MENU)
    await callback.answer()
