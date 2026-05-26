from aiogram import F, Router
from aiogram.types import CallbackQuery, Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import MENU_SEASON, season_actions_keyboard, seasons_list_keyboard
from bot.services import load_seasons_catalog, send_main_menu

router = Router()


def _season_card_text(season: dict, *, current_id: str | None) -> str:
    chapter_line = ""
    closed_line = texts.SEASON_CLOSED if season.get("isClosed") else ""
    current_line = texts.SEASON_CURRENT if current_id and str(season.get("id")) == current_id else ""
    return texts.SEASON.format(
        name=season.get("name", "Сезон"),
        number=season.get("number", ""),
        count=season.get("photoUsersCount", 0),
        chapter=chapter_line,
        closed=closed_line + current_line,
    )


async def send_seasons_list(
    target: Message,
    api: LooksRatingApiClient,
    telegram_id: int,
    *,
    edit: bool = False,
) -> None:
    seasons, current = await load_seasons_catalog(api)
    if not seasons:
        await send_main_menu(target, api, telegram_id, texts.SEASON_NOT_FOUND)
        return

    current_id = str(current["id"]) if current else None
    markup = seasons_list_keyboard(seasons, current_id)
    if edit:
        try:
            await target.edit_text(texts.SEASONS_LIST, reply_markup=markup)
            return
        except Exception:
            pass
    await target.answer(texts.SEASONS_LIST, reply_markup=markup)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_SEASON)
async def menu_seasons(message: Message, api: LooksRatingApiClient) -> None:
    await send_seasons_list(message, api, message.from_user.id)


@router.callback_query(F.data == "season:list")
async def seasons_list_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    if callback.message:
        await send_seasons_list(
            callback.message,
            api,
            callback.from_user.id,
            edit=True,
        )
    await callback.answer()


@router.callback_query(F.data.startswith("season:open:"))
async def season_open_callback(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    season_id = callback.data.removeprefix("season:open:")
    seasons, current = await load_seasons_catalog(api)
    season = next((item for item in seasons if str(item.get("id")) == season_id), None)
    if not season:
        await callback.answer(texts.SEASON_NOT_FOUND, show_alert=True)
        return

    current_id = str(current["id"]) if current else None
    text = _season_card_text(season, current_id=current_id)
    markup = season_actions_keyboard(season_id)
    if callback.message:
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
