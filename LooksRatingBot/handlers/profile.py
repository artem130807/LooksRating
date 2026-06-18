from aiogram import F, Router
from aiogram.types import Message
from aiogram.types import InputMediaPhoto

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import MENU_PROFILE
from bot.services import (
    AGE_ALL,
    format_city_display,
    format_feed_age_range,
    format_rating_display,
    format_sparks_amount,
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
        count_in_top = stats.get("countInTop", stats.get("timesInTop", user.get("countInTop", 0)))
        age = user.get("age")
        age_info = "Все возраста" if age == AGE_ALL else format_feed_age_range(age)
        stats_line = (
            "\n"
            + f"📐 Категория ленты по возрасту: <b>{age_info}</b>\n"
            + texts.STATS_IN_TOP.format(times=count_in_top)
            + "\n"
            + texts.STATS_SEASONS.format(count=stats.get("seasonsWithPhoto", 0))
        )
    except ApiError:
        pass

    payload = await api.get_my_photo(telegram_id) if user.get("hasPhoto") else None
    photos = list((payload or {}).get("photos") or [])
    has_photo = len(photos) > 0
    text = texts.PROFILE.format(
        display_name=user.get("displayName") or "—",
        photo="есть" if has_photo else "нет",
        vip_status="активен" if user.get("hasVip") else "не активен",
        sparks=format_sparks_amount(user.get("sparksBalance", 0)),
    ) + stats_line

    if has_photo:
        first = photos[0]
        if first:
            season_top_place = ""
            place = payload.get("seasonTopPlace")
            if place:
                season_top_place = texts.PROFILE_SEASON_TOP_PLACE.format(place=place)

            text += "\n\n" + texts.PROFILE_PHOTO_STATS.format(
                rating_line=format_rating_display(
                    float(first.get("rating", 0)),
                    int(first.get("ratingCount", 0)),
                ),
                season_top_place=season_top_place,
                rank=first.get("rank", "—"),
                city=format_city_display(first.get("city")),
                age=first.get("age", "—"),
                gender=first.get("gender", "—"),
            )
            media = [InputMediaPhoto(media=first["telegramFileId"], caption=text)]
            for photo in photos[1:]:
                file_id = photo.get("telegramFileId")
                if file_id:
                    media.append(InputMediaPhoto(media=file_id))
            await message.answer_media_group(media)
            return

    await send_main_menu(message, api, telegram_id, text)
