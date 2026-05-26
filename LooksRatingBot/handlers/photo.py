from aiogram import F, Router
from aiogram.filters import StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.keyboards import (
    BTN_NO,
    BTN_YES,
    MENU_CANCEL,
    cancel_keyboard,
    gender_keyboard,
    yes_no_photo_keyboard,
)
from bot.services import (
    SessionState,
    custom_nomination,
    format_api_error,
    format_city_display,
    format_rating_display,
    gender_from_text,
    load_cities,
    resolve_city_name,
    send_main_menu,
    send_settings_menu,
    set_bot_state,
)
from bot.states import PhotoStates, RecreatePhotoStates

router = Router()


async def offer_photo_after_registration(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    user = await api.get_user(telegram_id)
    if user and user.get("hasPhoto"):
        return
    await state.set_state(PhotoStates.confirm_create)
    await set_bot_state(api, telegram_id, SessionState.AWAITING_PHOTO)
    await message.answer(texts.PHOTO_OFFER, reply_markup=yes_no_photo_keyboard())


@router.message(PhotoStates.confirm_create, F.text == BTN_YES)
async def photo_yes(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    await start_custom_nomination_flow(message, state, api, recreate=False, from_settings=False)


@router.message(PhotoStates.confirm_create, F.text == BTN_NO)
async def photo_no(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    await state.clear()
    await set_bot_state(api, message.from_user.id, SessionState.IDLE)
    await send_main_menu(message, api, message.from_user.id, texts.PHOTO_LATER)


async def _finish_photo_flow(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
    text: str,
) -> None:
    data = await state.get_data()
    from_settings = data.get("from_settings", False)
    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.IDLE)
    if from_settings:
        await send_settings_menu(message, api, telegram_id, text)
    else:
        await send_main_menu(message, api, telegram_id, text)


async def start_nomination_flow(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    *,
    recreate: bool,
    from_settings: bool = False,
) -> None:
    await start_custom_nomination_flow(
        message,
        state,
        api,
        recreate=recreate,
        from_settings=from_settings,
    )


async def start_custom_nomination_flow(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    *,
    recreate: bool,
    from_settings: bool,
) -> None:
    try:
        cities = await load_cities(api)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    await state.clear()
    group = RecreatePhotoStates if recreate else PhotoStates
    await state.set_state(group.custom_city)
    await state.update_data(recreate=recreate, from_settings=from_settings, cities=cities)
    title = "Замените фото в сезоне" if recreate else "Добавьте фото в сезон"
    await message.answer(
        f"<b>{title}</b>\n\n{texts.PHOTO_NOM_CITY}",
        reply_markup=cancel_keyboard(),
    )


@router.message(PhotoStates.custom_city, F.text)
@router.message(RecreatePhotoStates.custom_city, F.text)
async def nomination_custom_city(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text == MENU_CANCEL:
        await _finish_photo_flow(message, state, api, message.from_user.id, texts.PHOTO_CANCEL)
        return
    data = await state.get_data()
    cities: list[str] = data.get("cities", [])
    city = resolve_city_name(message.text, cities)
    if city is None:
        await message.answer(texts.REG_CITY_NOT_FOUND, reply_markup=cancel_keyboard())
        return
    await state.update_data(nom_city=city)
    data = await state.get_data()
    group = RecreatePhotoStates if data.get("recreate") else PhotoStates
    await state.set_state(group.custom_age)
    await message.answer(texts.PHOTO_NOM_AGE, reply_markup=cancel_keyboard())


@router.message(PhotoStates.custom_age, F.text)
@router.message(RecreatePhotoStates.custom_age, F.text)
async def nomination_custom_age(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text == MENU_CANCEL:
        await _finish_photo_flow(message, state, api, message.from_user.id, texts.PHOTO_CANCEL)
        return
    try:
        age = int(message.text.strip())
    except ValueError:
        await message.answer(texts.REG_AGE_INVALID)
        return
    if age < 14 or age > 100:
        await message.answer(texts.REG_AGE_INVALID)
        return
    await state.update_data(nom_age=age)
    data = await state.get_data()
    group = RecreatePhotoStates if data.get("recreate") else PhotoStates
    await state.set_state(group.custom_gender)
    await message.answer(texts.PHOTO_NOM_GENDER, reply_markup=gender_keyboard())


@router.message(PhotoStates.custom_gender, F.text)
@router.message(RecreatePhotoStates.custom_gender, F.text)
async def nomination_custom_gender(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text == MENU_CANCEL:
        await _finish_photo_flow(message, state, api, message.from_user.id, texts.PHOTO_CANCEL)
        return
    gender = gender_from_text(message.text)
    if gender is None:
        await message.answer(texts.REG_GENDER_INVALID, reply_markup=gender_keyboard())
        return
    data = await state.get_data()
    nomination = custom_nomination(data["nom_city"], data["nom_age"], gender)
    await state.update_data(nomination=nomination)
    await go_upload(message, state, api)


async def go_upload(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    data = await state.get_data()
    group = RecreatePhotoStates if data.get("recreate") else PhotoStates
    await state.set_state(group.upload)
    await message.answer(texts.PHOTO_UPLOAD, reply_markup=cancel_keyboard())


@router.message(
    StateFilter(PhotoStates, RecreatePhotoStates),
    F.text == MENU_CANCEL,
)
async def photo_flow_cancel(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    await _finish_photo_flow(message, state, api, message.from_user.id, texts.PHOTO_CANCEL)


@router.message(PhotoStates.upload, F.photo)
@router.message(RecreatePhotoStates.upload, F.photo)
async def photo_uploaded(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    data = await state.get_data()
    file_id = message.photo[-1].file_id
    nomination = data.get("nomination")
    telegram_id = message.from_user.id
    recreate = data.get("recreate", False)

    try:
        if recreate:
            result = await api.recreate_photo(telegram_id, file_id, nomination)
        else:
            result = await api.set_photo(telegram_id, file_id, nomination)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        await _finish_photo_flow(message, state, api, telegram_id, texts.MAIN_MENU)
        return

    city = result.get("city", "")
    await _finish_photo_flow(
        message,
        state,
        api,
        telegram_id,
        texts.PHOTO_SAVED.format(
            city=format_city_display(city),
            rating_line=format_rating_display(float(result.get("rating", 0)), 0),
        ),
    )
