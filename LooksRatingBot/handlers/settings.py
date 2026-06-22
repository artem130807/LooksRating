from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.filters import NOT_DURING_RATING_OR_TICKET
from bot.keyboards import (
    BTN_DELETE_ACCOUNT,
    BTN_DELETE_CONFIRM,
    BTN_EDIT_AGE,
    BTN_AGE_ALL,
    BTN_EDIT_CITY,
    BTN_EDIT_GENDER,
    BTN_HIDE_TELEGRAM_USERNAME,
    BTN_SHOW_TELEGRAM_USERNAME,
    BTN_SETTINGS_FEED,
    MENU_PHOTO_ADD,
    MENU_BACK,
    MENU_CANCEL,
    MENU_PHOTO_REPLACE,
    MENU_PHOTO_REPLACE_ALL,
    MENU_SETTINGS,
    SETTINGS_PHOTO_BUTTONS,
    age_input_keyboard,
    cancel_keyboard,
    delete_account_keyboard,
    feed_gender_keyboard,
)
from bot.age_rules import parse_feed_age_text
from bot.services import (
    SessionState,
    feed_gender_from_text,
    format_api_error,
    format_feed_age_value,
    format_feed_age_range,
    format_city_display,
    load_cities,
    resolve_city_name,
    resolve_display_preference_action,
    send_feed_view,
    send_main_menu,
    send_settings_menu,
    set_bot_state,
)
from bot.states import ProfileEditStates, SettingsStates
from handlers.photo import start_nomination_flow
from handlers.registration import begin_registration

router = Router()


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_SETTINGS)
async def open_settings(message: Message, api: LooksRatingApiClient) -> None:
    telegram_id = message.from_user.id
    user = await api.get_user(telegram_id)
    if not user:
        await message.answer(texts.NEED_START)
        return
    await send_settings_menu(message, api, telegram_id, user=user)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == BTN_HIDE_TELEGRAM_USERNAME)
async def hide_telegram_username_start(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    user = await api.get_user(message.from_user.id)
    if not user:
        await message.answer(texts.NEED_START)
        return
    if resolve_display_preference_action(user) != "hide":
        await send_settings_menu(message, api, message.from_user.id, user=user)
        return

    await state.set_state(SettingsStates.hide_display_name)
    await message.answer(texts.SETTINGS_HIDE_USERNAME_PROMPT, reply_markup=cancel_keyboard())


@router.message(SettingsStates.hide_display_name, F.text)
async def hide_telegram_username_save(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    if message.text in {MENU_CANCEL, MENU_BACK}:
        await state.clear()
        await send_settings_menu(message, api, message.from_user.id)
        return

    display_name = (message.text or "").strip()
    if not display_name or len(display_name) > 32:
        await message.answer(texts.REG_DISPLAY_NAME_INVALID, reply_markup=cancel_keyboard())
        return

    try:
        result = await api.update_display_preference(
            message.from_user.id,
            telegram_username=message.from_user.username,
            use_telegram_username_as_display=False,
            custom_name=display_name,
        )
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    await state.clear()
    resolved_name = result.get("displayName") or result.get("DisplayName") or display_name
    await send_settings_menu(
        message,
        api,
        message.from_user.id,
        texts.SETTINGS_HIDE_USERNAME_DONE.format(display_name=resolved_name),
    )


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == BTN_SHOW_TELEGRAM_USERNAME)
async def show_telegram_username(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    if not message.from_user.username:
        await message.answer(texts.SETTINGS_NO_TELEGRAM_USERNAME)
        return

    user = await api.get_user(message.from_user.id)
    if not user:
        await message.answer(texts.NEED_START)
        return
    if resolve_display_preference_action(user) != "show":
        await send_settings_menu(message, api, message.from_user.id, user=user)
        return

    try:
        await api.update_display_preference(
            message.from_user.id,
            telegram_username=message.from_user.username,
            use_telegram_username_as_display=True,
        )
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    await state.clear()
    await send_settings_menu(
        message,
        api,
        message.from_user.id,
        texts.SETTINGS_SHOW_USERNAME_DONE,
    )


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == MENU_BACK)
async def settings_back(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    current = await state.get_state()
    if current == SettingsStates.confirm_delete.state:
        await state.clear()
        await send_settings_menu(message, api, message.from_user.id)
        return

    if current == SettingsStates.hide_display_name.state:
        await state.clear()
        await send_settings_menu(message, api, message.from_user.id)
        return

    if current and current.startswith("ProfileEditStates"):
        if current == ProfileEditStates.field.state:
            await state.clear()
            await send_settings_menu(message, api, message.from_user.id)
            return
        await state.set_state(ProfileEditStates.field)
        await send_feed_view(message, api, message.from_user.id)
        return

    await state.clear()
    await send_main_menu(message, api, message.from_user.id, texts.MAIN_MENU)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == BTN_SETTINGS_FEED)
async def settings_feed(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    user = await api.get_user(message.from_user.id)
    if not user:
        await message.answer(texts.NEED_START)
        return
    if not user.get("hasRecommendationSettings"):
        from handlers.feed_setup import begin_feed_setup

        await begin_feed_setup(
            message,
            state,
            api,
            message.from_user.id,
            start_rating_after=False,
            from_settings=True,
        )
        return
    await state.set_state(ProfileEditStates.field)
    await send_feed_view(message, api, message.from_user.id)


@router.message(ProfileEditStates.field, F.text == BTN_EDIT_CITY)
async def edit_city_start(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    try:
        cities = await load_cities(api)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return
    if not cities:
        await message.answer(texts.REG_CITIES_EMPTY)
        return
    await state.update_data(cities=cities)
    await state.set_state(ProfileEditStates.city)
    await message.answer(texts.EDIT_CITY, reply_markup=cancel_keyboard())


@router.message(ProfileEditStates.city, F.text)
async def edit_city_save(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text in {MENU_CANCEL, MENU_BACK}:
        await state.set_state(ProfileEditStates.field)
        await send_feed_view(message, api, message.from_user.id)
        return
    data = await state.get_data()
    cities: list[str] = data.get("cities", [])
    city = resolve_city_name(message.text, cities)
    if city is None:
        await message.answer(texts.REG_CITY_NOT_FOUND, reply_markup=cancel_keyboard())
        return
    try:
        await api.update_city(message.from_user.id, city)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return
    await state.set_state(ProfileEditStates.field)
    await send_feed_view(
        message,
        api,
        message.from_user.id,
        prefix=texts.EDIT_SAVED_CITY.format(city=format_city_display(city)) + "\n\n",
    )


@router.message(ProfileEditStates.field, F.text == BTN_EDIT_AGE)
async def edit_age_start(message: Message, state: FSMContext) -> None:
    await state.set_state(ProfileEditStates.age)
    await message.answer(texts.EDIT_AGE, reply_markup=age_input_keyboard())


@router.message(ProfileEditStates.field, F.text == BTN_EDIT_GENDER)
async def edit_gender_start(message: Message, state: FSMContext) -> None:
    await state.set_state(ProfileEditStates.gender)
    await message.answer(texts.EDIT_GENDER, reply_markup=feed_gender_keyboard())


@router.message(ProfileEditStates.age, F.text)
async def edit_age_save(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text in {MENU_CANCEL, MENU_BACK}:
        await state.set_state(ProfileEditStates.field)
        await send_feed_view(message, api, message.from_user.id)
        return
    age = parse_feed_age_text(message.text, all_ages_button=BTN_AGE_ALL)
    if age is None:
        await message.answer(texts.REG_AGE_INVALID, reply_markup=age_input_keyboard())
        return
    try:
        await api.update_age(message.from_user.id, age)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return
    await state.set_state(ProfileEditStates.field)
    await send_feed_view(
        message,
        api,
        message.from_user.id,
        prefix=(
            texts.EDIT_SAVED_AGE.format(age=format_feed_age_value(age))
            + "\n"
            + texts.FEED_SETUP_AGE_RANGE.format(age_range=format_feed_age_range(age))
            + "\n\n"
        ),
    )


@router.message(ProfileEditStates.gender, F.text)
async def edit_gender_save(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text in {MENU_CANCEL, MENU_BACK}:
        await state.set_state(ProfileEditStates.field)
        await send_feed_view(message, api, message.from_user.id)
        return
    gender = feed_gender_from_text(message.text)
    if gender is None:
        await message.answer(texts.REG_GENDER_INVALID, reply_markup=feed_gender_keyboard())
        return
    try:
        await api.update_gender(message.from_user.id, gender)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return
    await state.set_state(ProfileEditStates.field)
    await send_feed_view(
        message,
        api,
        message.from_user.id,
        prefix=texts.EDIT_SAVED_GENDER + "\n\n",
    )


@router.message(NOT_DURING_RATING_OR_TICKET, F.text.in_(SETTINGS_PHOTO_BUTTONS))
async def settings_photo(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    telegram_id = message.from_user.id
    user = await api.get_user(telegram_id)
    if not user:
        await message.answer(texts.NEED_START)
        return
    if message.text == MENU_PHOTO_REPLACE_ALL:
        await start_nomination_flow(
            message,
            state,
            api,
            recreate=True,
            from_settings=True,
            replace_all=True,
        )
        return

    recreate = message.text == MENU_PHOTO_REPLACE
    if message.text == MENU_PHOTO_ADD:
        photo_payload = await api.get_my_photo(telegram_id)
        if isinstance(photo_payload, dict) and not photo_payload.get("canAddPhoto", False):
            await send_settings_menu(message, api, telegram_id, texts.VIP_PHOTO_LIMIT)
            return
        recreate = False
    await start_nomination_flow(message, state, api, recreate=recreate, from_settings=True)


@router.message(NOT_DURING_RATING_OR_TICKET, F.text == BTN_DELETE_ACCOUNT)
async def delete_account_prompt(message: Message, state: FSMContext) -> None:
    await state.set_state(SettingsStates.confirm_delete)
    await message.answer(texts.DELETE_ACCOUNT_CONFIRM, reply_markup=delete_account_keyboard())


@router.message(SettingsStates.confirm_delete, F.text == BTN_DELETE_CONFIRM)
async def delete_account_confirm(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    telegram_id = message.from_user.id
    username = message.from_user.username
    try:
        await api.delete_account(telegram_id)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        await state.clear()
        await send_settings_menu(message, api, telegram_id)
        return

    await state.clear()
    await set_bot_state(api, telegram_id, SessionState.IDLE)
    await message.answer(texts.DELETE_ACCOUNT_DONE)
    await begin_registration(message, state, api, telegram_id, username)


@router.message(SettingsStates.confirm_delete, F.text)
async def delete_account_other(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    if message.text == MENU_BACK:
        await state.clear()
        await send_settings_menu(message, api, message.from_user.id)
        return
    await message.answer(texts.DELETE_ACCOUNT_CONFIRM, reply_markup=delete_account_keyboard())
