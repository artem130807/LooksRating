from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import MENU_MY_PHOTO, MENU_RATE, rating_flow_keyboard
from bot.services import SessionState, format_city_display, format_rating_display, main_menu_for, send_main_menu, set_bot_state
from handlers.feed_setup import begin_feed_setup
from handlers.rating import show_next_photo

router = Router()


async def start_rating_after_feed_setup(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.RATING)
    await message.answer(texts.RATING_START, reply_markup=rating_flow_keyboard())
    await show_next_photo(message, state, api, telegram_id)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_RATE)
async def menu_rate(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    telegram_id = message.from_user.id
    user = await api.get_user(telegram_id)
    if not user:
        await message.answer(texts.NEED_START)
        return

    if not user.get("hasRecommendationSettings"):
        await begin_feed_setup(message, state, api, telegram_id, start_rating_after=True)
        return

    await start_rating_after_feed_setup(message, state, api, telegram_id)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_MY_PHOTO)
async def menu_my_photo(message: Message, api: LooksRatingApiClient) -> None:
    telegram_id = message.from_user.id
    photo = await api.get_my_photo(telegram_id)
    if not photo:
        await send_main_menu(message, api, telegram_id, texts.MY_PHOTO_EMPTY)
        return
    caption = texts.MY_PHOTO.format(
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
        caption=caption,
        reply_markup=await main_menu_for(api, telegram_id),
    )
