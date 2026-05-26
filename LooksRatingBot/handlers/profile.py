from aiogram import F, Router
from aiogram.types import Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import MENU_PROFILE
from bot.services import (
    format_city_display,
    format_rating_display,
    main_menu_for,
    send_main_menu,
)

router = Router()


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_PROFILE)
async def show_profile(message: Message, api: LooksRatingApiClient) -> None:
    telegram_id = message.from_user.id
    user = await api.get_user(telegram_id)
    if not user:
        await message.answer(texts.NEED_START)
        return

    stats_line = ""
    try:
        stats = await api.get_user_stats(telegram_id)
        stats_line = (
            "\n"
            + texts.STATS_IN_TOP.format(times=stats.get("timesInTop", 0))
            + "\n"
            + texts.STATS_SEASONS.format(count=stats.get("seasonsWithPhoto", 0))
        )
    except ApiError:
        pass

    has_photo = bool(user.get("hasPhoto"))
    text = texts.PROFILE.format(
        display_name=user.get("displayName") or "—",
        photo="есть" if has_photo else "нет",
    ) + stats_line

    if has_photo:
        photo = await api.get_my_photo(telegram_id)
        if photo:
            text += "\n\n" + texts.PROFILE_PHOTO_STATS.format(
                rating_line=format_rating_display(
                    float(photo.get("rating", 0)),
                    int(photo.get("ratingCount", 0)),
                ),
                rank=photo.get("rank", "—"),
                city=format_city_display(photo.get("city")),
                age=photo.get("age", "—"),
                gender=photo.get("gender", "—"),
            )
            await message.answer_photo(
                photo["telegramFileId"],
                caption=text,
                reply_markup=await main_menu_for(api, telegram_id),
            )
            return

    await send_main_menu(message, api, telegram_id, text)
