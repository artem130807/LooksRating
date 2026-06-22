from aiogram import F, Router
from aiogram.filters import StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

import logging

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.photo_hints import photo_settings_intro
from bot.keyboards import (
    BTN_NO,
    BTN_YES,
    MENU_CANCEL,
    cancel_keyboard,
    gender_keyboard,
    multi_photo_upload_keyboard,
    replace_photo_picker_keyboard,
    yes_no_photo_keyboard,
)
from bot.age_rules import parse_nomination_age_text
from bot.services import (
    SessionState,
    custom_nomination,
    format_api_error,
    format_set_photo_saved_text,
    gender_from_text,
    load_cities,
    resolve_city_name,
    send_main_menu,
    send_settings_menu,
    set_bot_state,
)
from bot.photo_upload import (
    PHOTO_UPLOAD_STATE,
    VIDEO_UPLOAD,
    reply_photo_upload_required,
)
from bot.states import PhotoStates, RecreatePhotoStates

logger = logging.getLogger(__name__)
router = Router()


async def offer_photo_after_registration(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
) -> None:
    payload = await api.get_my_photo(telegram_id)
    photos = list((payload or {}).get("photos") or [])
    if photos:
        return
    await offer_photo_creation_prompt(
        message,
        state,
        api,
        telegram_id,
        texts.PHOTO_OFFER,
    )


async def offer_photo_creation_prompt(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
    text: str,
) -> None:
    await state.set_state(PhotoStates.confirm_create)
    await set_bot_state(api, telegram_id, SessionState.AWAITING_PHOTO)
    await message.answer(text, reply_markup=yes_no_photo_keyboard())


@router.message(PhotoStates.confirm_create, F.text == BTN_YES)
async def photo_yes(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    await start_nomination_flow(message, state, api, recreate=False, from_settings=False)


@router.message(PhotoStates.confirm_create, F.text == BTN_NO)
async def photo_no(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    await state.clear()
    await set_bot_state(api, message.from_user.id, SessionState.IDLE)
    await send_main_menu(message, api, message.from_user.id, texts.MAIN_MENU)


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
    try:
        await set_bot_state(api, telegram_id, SessionState.IDLE)
    except ApiError as exc:
        logger.warning("Failed to reset session after photo flow for %s: %s", telegram_id, exc.message)
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
    replace_all: bool = False,
) -> None:
    await start_custom_nomination_flow(
        message,
        state,
        api,
        recreate=recreate,
        from_settings=from_settings,
        replace_all=replace_all,
    )


async def start_custom_nomination_flow(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    *,
    recreate: bool = False,
    from_settings: bool = False,
    replace_all: bool = False,
) -> None:
    try:
        cities = await load_cities(api)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        return

    intro = ""
    if from_settings:
        user = await api.get_user(message.from_user.id)
        has_vip = bool(user and user.get("hasVip"))
        intro = photo_settings_intro(
            has_vip=has_vip,
            recreate=recreate,
            replace_all=replace_all,
        )
        if intro:
            intro = intro + "\n\n"

    existing_photos: list[dict] = []
    if recreate:
        try:
            payload = await api.get_my_photo(message.from_user.id)
        except ApiError as exc:
            await message.answer(format_api_error(exc))
            return
        existing_photos = list((payload or {}).get("photos") or [])
        existing_photos = [p for p in existing_photos if p.get("id") and p.get("telegramFileId")]
        if not existing_photos:
            await message.answer(texts.PHOTO_NOT_FOUND)
            return

    await state.clear()
    group = RecreatePhotoStates if recreate else PhotoStates
    await state.update_data(
        recreate=recreate,
        from_settings=from_settings,
        replace_all=replace_all,
        cities=cities,
        existing_photos=existing_photos,
    )
    if recreate and replace_all:
        await state.set_state(RecreatePhotoStates.custom_city)
        await message.answer(
            f"{intro}<b>Смените все фото в сезоне</b>\n\n{texts.PHOTO_NOM_CITY}",
            reply_markup=cancel_keyboard(),
        )
        return

    if recreate and len(existing_photos) > 1:
        await state.set_state(RecreatePhotoStates.select_target)
        await message.answer(
            f"{intro}{texts.PHOTO_REPLACE_PICK}",
            reply_markup=replace_photo_picker_keyboard(existing_photos),
        )
        return

    if recreate and existing_photos:
        await state.update_data(target_photo_id=str(existing_photos[0]["id"]))
    await state.set_state(group.custom_city)
    title = "Замените фото в сезоне" if recreate else "Добавьте фото в сезон"
    await message.answer(
        f"{intro}<b>{title}</b>\n\n{texts.PHOTO_NOM_CITY}",
        reply_markup=cancel_keyboard(),
    )


@router.callback_query(RecreatePhotoStates.select_target, F.data == "replace:cancel")
async def recreate_pick_cancel(callback: CallbackQuery, state: FSMContext, api: LooksRatingApiClient) -> None:
    if callback.message:
        await _finish_photo_flow(callback.message, state, api, callback.from_user.id, texts.PHOTO_CANCEL)
    await callback.answer()


@router.callback_query(RecreatePhotoStates.select_target, F.data.startswith("replace:pick:"))
async def recreate_pick_photo(callback: CallbackQuery, state: FSMContext) -> None:
    target_photo_id = callback.data.split(":", 2)[2]
    await state.update_data(target_photo_id=target_photo_id)
    await state.set_state(RecreatePhotoStates.custom_city)
    if callback.message:
        await callback.message.answer(texts.PHOTO_NOM_CITY, reply_markup=cancel_keyboard())
    await callback.answer()


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
    age = parse_nomination_age_text(message.text)
    if age is None:
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
    replace_all = bool(data.get("replace_all"))
    if replace_all:
        await state.set_state(RecreatePhotoStates.upload_many)
        await state.update_data(replace_all_file_ids=[])
        await message.answer(texts.PHOTO_REPLACE_ALL_UPLOAD, reply_markup=multi_photo_upload_keyboard())
        return

    group = RecreatePhotoStates if data.get("recreate") else PhotoStates
    await state.set_state(group.upload)
    await message.answer(texts.PHOTO_UPLOAD, reply_markup=cancel_keyboard())


@router.message(
    StateFilter(PhotoStates, RecreatePhotoStates),
    F.text == MENU_CANCEL,
)
async def photo_flow_cancel(message: Message, state: FSMContext, api: LooksRatingApiClient) -> None:
    await _finish_photo_flow(message, state, api, message.from_user.id, texts.PHOTO_CANCEL)


@router.message(PHOTO_UPLOAD_STATE, VIDEO_UPLOAD)
async def photo_upload_reject_video(message: Message, state: FSMContext) -> None:
    await reply_photo_upload_required(message, state, text=texts.PHOTO_VIDEO_NOT_ALLOWED)


@router.message(PHOTO_UPLOAD_STATE, F.document.mime_type.startswith("video"))
async def photo_upload_reject_video_document(message: Message, state: FSMContext) -> None:
    await reply_photo_upload_required(message, state, text=texts.PHOTO_VIDEO_NOT_ALLOWED)


@router.message(PhotoStates.upload, F.photo)
@router.message(RecreatePhotoStates.upload, F.photo)
async def photo_uploaded(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.media_group_id:
        await message.answer("Отправьте одно фото одним сообщением (не альбом).")
        return

    data = await state.get_data()
    file_id = message.photo[-1].file_id
    nomination = data.get("nomination")
    telegram_id = message.from_user.id
    recreate = data.get("recreate", False)
    target_photo_id = data.get("target_photo_id")

    if not nomination:
        await message.answer(
            "Данные номинации потеряны. Начните добавление фото заново в «⚙️ Настройки».",
            reply_markup=cancel_keyboard(),
        )
        await _finish_photo_flow(message, state, api, telegram_id, texts.MAIN_MENU)
        return

    status = await message.answer("⏳ Сохраняю фото…")

    try:
        if recreate:
            result = await api.recreate_photo(
                telegram_id,
                file_id,
                nomination,
                target_photo_id=target_photo_id,
            )
        else:
            result = await api.set_photo(telegram_id, file_id, nomination)
    except ApiError as exc:
        await status.edit_text(format_api_error(exc))
        await _finish_photo_flow(message, state, api, telegram_id, texts.MAIN_MENU)
        return
    except Exception:
        logger.exception("Unexpected error while saving photo for %s", telegram_id)
        await status.edit_text("Не удалось сохранить фото. Попробуйте ещё раз из «⚙️ Настройки».")
        await _finish_photo_flow(message, state, api, telegram_id, texts.MAIN_MENU)
        return

    try:
        await status.delete()
    except Exception:
        pass

    await _finish_photo_flow(
        message,
        state,
        api,
        telegram_id,
        format_set_photo_saved_text(result),
    )


@router.message(RecreatePhotoStates.upload_many, F.photo)
async def photo_uploaded_many(
    message: Message, state: FSMContext
) -> None:
    if message.media_group_id:
        await message.answer("Отправьте одно фото одним сообщением (не альбом).")
        return

    data = await state.get_data()
    file_ids = list(data.get("replace_all_file_ids") or [])
    if len(file_ids) >= 4:
        await message.answer(texts.PHOTO_REPLACE_ALL_LIMIT, reply_markup=multi_photo_upload_keyboard())
        return

    file_ids.append(message.photo[-1].file_id)
    await state.update_data(replace_all_file_ids=file_ids)
    await message.answer(
        f"Добавлено фото: {len(file_ids)}/4",
        reply_markup=multi_photo_upload_keyboard(),
    )


@router.message(RecreatePhotoStates.upload_many, F.text == "✅ Сохранить набор")
async def photo_uploaded_many_save(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    data = await state.get_data()
    telegram_id = message.from_user.id
    nomination = data.get("nomination")
    file_ids = list(data.get("replace_all_file_ids") or [])
    if not file_ids:
        await message.answer(texts.PHOTO_REPLACE_ALL_EMPTY, reply_markup=multi_photo_upload_keyboard())
        return

    try:
        result = await api.recreate_all_photos(telegram_id, file_ids, nomination)
    except ApiError as exc:
        await message.answer(format_api_error(exc))
        await _finish_photo_flow(message, state, api, telegram_id, texts.MAIN_MENU)
        return

    await _finish_photo_flow(
        message,
        state,
        api,
        telegram_id,
        format_set_photo_saved_text(result),
    )
