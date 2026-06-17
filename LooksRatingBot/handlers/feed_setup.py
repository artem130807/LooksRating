import logging

from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.keyboards import (
    BTN_AGE_ALL,
    MENU_CANCEL,
    age_input_keyboard,
    cancel_keyboard,
    feed_gender_keyboard,
    remove_keyboard,
)
from bot.services import (
    AGE_ALL,
    SessionState,
    feed_gender_from_text,
    format_api_error,
    format_feed_age_value,
    format_feed_age_range,
    format_city_display,
    gender_label,
    load_cities,
    resolve_city_name,
    set_bot_state,
)
from bot.states import FeedSetupStates, ProfileEditStates

logger = logging.getLogger(__name__)
router = Router()


async def begin_feed_setup(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
    *,
    start_rating_after: bool = False,
    from_settings: bool = False,
) -> None:
    try:
        cities = await load_cities(api)
    except ApiError as exc:
        logger.error("Failed to load cities: %s", exc.message)
        await message.answer(format_api_error(exc))
        return

    if not cities:
        await message.answer(texts.REG_CITIES_EMPTY)
        return

    await state.set_state(FeedSetupStates.city)
    await state.update_data(
        cities=cities,
        feed_setup=True,
        start_rating_after=start_rating_after,
        from_settings=from_settings,
    )
    try:
        await set_bot_state(api, telegram_id, SessionState.AWAITING_FEED_CITY)
    except ApiError as exc:
        logger.error("Failed to set feed setup state: %s", exc.message)
        await message.answer(format_api_error(exc))
        return

    await message.answer(texts.FEED_SETUP_CITY, reply_markup=cancel_keyboard())


@router.message(FeedSetupStates.city, F.text)
async def feed_city_entered(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    telegram_id = message.from_user.id
    if message.text == MENU_CANCEL:
        await state.clear()
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await message.answer(texts.FEED_SETUP_CANCEL, reply_markup=remove_keyboard())
        return

    data = await state.get_data()
    cities: list[str] = data.get("cities", [])
    if not cities:
        try:
            cities = await load_cities(api)
            await state.update_data(cities=cities)
        except ApiError as exc:
            await message.answer(format_api_error(exc))
            return

    city = resolve_city_name(message.text, cities)
    if city is None:
        await message.answer(texts.REG_CITY_NOT_FOUND, reply_markup=cancel_keyboard())
        return

    await state.update_data(city=city)
    await state.set_state(FeedSetupStates.age)
    try:
        await set_bot_state(api, telegram_id, SessionState.AWAITING_FEED_AGE)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    await message.answer(
        texts.FEED_SETUP_AGE.format(city=format_city_display(city)),
        reply_markup=age_input_keyboard(),
    )


@router.message(FeedSetupStates.age, F.text)
async def feed_age_entered(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    telegram_id = message.from_user.id
    if message.text == MENU_CANCEL:
        await state.clear()
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await message.answer(texts.FEED_SETUP_CANCEL, reply_markup=remove_keyboard())
        return

    try:
        if message.text.strip() == BTN_AGE_ALL:
            age = AGE_ALL
        else:
            age = int(message.text.strip())
    except ValueError:
        await message.answer(texts.REG_AGE_INVALID, reply_markup=age_input_keyboard())
        return
    if age != AGE_ALL and (age < 14 or age > 100):
        await message.answer(texts.REG_AGE_INVALID, reply_markup=age_input_keyboard())
        return

    await state.update_data(age=age)
    await state.set_state(FeedSetupStates.gender)
    try:
        await set_bot_state(api, telegram_id, SessionState.AWAITING_FEED_GENDER)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    await message.answer(
        texts.FEED_SETUP_AGE_RANGE.format(age_range=format_feed_age_range(age))
        + "\n\n"
        + texts.FEED_SETUP_GENDER,
        reply_markup=feed_gender_keyboard(),
    )


@router.message(FeedSetupStates.gender, F.text)
async def feed_gender_entered(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    telegram_id = message.from_user.id
    if message.text == MENU_CANCEL:
        await state.clear()
        await set_bot_state(api, telegram_id, SessionState.IDLE)
        await message.answer(texts.FEED_SETUP_CANCEL, reply_markup=remove_keyboard())
        return

    gender = feed_gender_from_text(message.text)
    if gender is None:
        await message.answer(texts.REG_GENDER_INVALID, reply_markup=feed_gender_keyboard())
        return

    data = await state.get_data()
    city = data["city"]
    age = data["age"]

    try:
        await api.upsert_recommendation_settings(telegram_id, age, gender, city)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.IDLE)
    gender_text = gender_label(gender)
    await message.answer(
        texts.FEED_SETUP_DONE.format(
            city=format_city_display(city),
            age=format_feed_age_value(age),
            gender=gender_text,
        )
    )

    if data.get("start_rating_after"):
        from handlers.menu import start_rating_after_feed_setup

        await start_rating_after_feed_setup(message, state, api, telegram_id)
    elif data.get("from_settings"):
        from bot.services import send_feed_view

        await state.set_state(ProfileEditStates.field)
        await send_feed_view(
            message,
            api,
            telegram_id,
            prefix=texts.FEED_SETUP_DONE.format(
                city=format_city_display(city),
                age=format_feed_age_value(age),
                gender=gender_text,
            )
            + "\n\n",
        )
    else:
        from bot.services import send_main_menu

        await send_main_menu(message, api, telegram_id, texts.MAIN_MENU)
