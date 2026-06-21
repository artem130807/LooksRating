import logging

from aiogram import F, Router
from aiogram.exceptions import TelegramBadRequest
from aiogram.filters import Command
from aiogram.fsm.context import FSMContext
from aiogram.types import BufferedInputFile, CallbackQuery, InputMediaPhoto, Message, ReplyKeyboardRemove

from api.client import ApiError, TicketApiClient
from bot import keyboards, texts
from bot.states import ModerationStates
from bot.telegram_media import MainBotMediaError, MainBotMediaService

router = Router()
logger = logging.getLogger(__name__)


def _pick(data: dict, *keys: str, default=None):
    for key in keys:
        value = data.get(key)
        if value is not None and value != "":
            return value
    return default


def _to_int(value) -> int | None:
    if value is None or value == "":
        return None
    try:
        return int(value)
    except (TypeError, ValueError):
        return None


def _normalize_ticket(payload: dict) -> dict:
    ticket = payload.get("ticket") or {}
    photos_raw = ticket.get("photos") or ticket.get("Photos") or []
    photos = []
    for item in photos_raw:
        file_id = _pick(item, "telegramFileId", "TelegramFileId")
        if file_id:
            photos.append(
                {
                    "id": _pick(item, "id", "Id", "photoId", "PhotoId"),
                    "telegramFileId": file_id,
                }
            )
    return {
        "id": _pick(ticket, "id", "Id", "ticketId", "TicketId"),
        "description": _pick(ticket, "description", "Description", default=""),
        "reporterDisplayName": _pick(ticket, "reporterDisplayName", "ReporterDisplayName", default="—"),
        "reporterTelegramId": _to_int(_pick(ticket, "reporterTelegramId", "ReporterTelegramId")),
        "reporterCity": _pick(ticket, "reporterCity", "ReporterCity", default="—"),
        "profileDisplayName": _pick(ticket, "profileDisplayName", "ProfileDisplayName", default="участник"),
        "profileTelegramId": _to_int(_pick(ticket, "profileTelegramId", "ProfileTelegramId")),
        "profileCity": _pick(ticket, "profileCity", "ProfileCity", default="—"),
        "profileGender": _pick(ticket, "profileGender", "ProfileGender", default="—"),
        "profileAge": _pick(ticket, "profileAge", "ProfileAge", default="—"),
        "profileRank": _pick(ticket, "profileRank", "ProfileRank", default="—"),
        "profileRating": _pick(ticket, "profileRating", "ProfileRating", default=0),
        "profileRatingCount": _pick(ticket, "profileRatingCount", "ProfileRatingCount", default=0),
        "photos": photos,
        "city": _pick(payload, "city", "City"),
        "remaining": int(payload.get("remaining") or payload.get("Remaining") or 0),
    }


async def show_city_selection(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    telegram_id: int,
) -> None:
    try:
        cities = await api.list_cities(telegram_id)
    except ApiError as exc:
        await message.answer(
            f"Не удалось загрузить города: {exc.message}",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    if not cities:
        await state.set_state(ModerationStates.moderating)
        await message.answer(
            texts.NO_CITIES,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    await state.set_state(ModerationStates.selecting_city)
    await state.update_data(cities=cities)
    await message.answer(
        "Выберите город с активными жалобами:",
        reply_markup=keyboards.admin_reply_keyboard(),
    )
    await message.answer(
        "Список городов:",
        reply_markup=keyboards.city_selection(cities),
    )


async def _prepare_ticket_photo_media(
    main_bot_media: MainBotMediaService,
    photos: list[dict],
) -> list[BufferedInputFile]:
    prepared: list[BufferedInputFile] = []
    for index, photo in enumerate(photos):
        file_id = photo.get("telegramFileId")
        if not file_id:
            continue
        try:
            content = await main_bot_media.download_photo_bytes(file_id)
        except MainBotMediaError as exc:
            logger.warning("moderation photo download failed for %s: %s", file_id[:24], exc)
            continue
        prepared.append(BufferedInputFile(content, filename=f"profile_{index + 1}.jpg"))
    return prepared


async def _send_ticket_photos(
    message: Message,
    *,
    caption: str,
    photos: list[dict],
    actions,
    main_bot_media: MainBotMediaService,
) -> bool:
    media_files = await _prepare_ticket_photo_media(main_bot_media, photos)
    if not media_files:
        return False

    if len(media_files) == 1:
        await message.answer_photo(media_files[0], caption=caption, reply_markup=actions)
        return True

    items = [InputMediaPhoto(media=media_files[0], caption=caption)]
    items.extend(InputMediaPhoto(media=item) for item in media_files[1:])
    await message.answer_media_group(items)
    await message.answer("Действия по жалобе:", reply_markup=actions)
    return True


async def present_current_ticket(
    message: Message,
    api: TicketApiClient,
    telegram_id: int,
    *,
    main_bot_media: MainBotMediaService,
    state: FSMContext | None = None,
    notice: str | None = None,
) -> None:
    if state is not None:
        await state.set_state(ModerationStates.moderating)

    if notice:
        await message.answer(notice, reply_markup=keyboards.admin_reply_keyboard())

    status = await message.answer(texts.LOADING_TICKET)

    try:
        payload = await api.get_current_ticket(telegram_id)
    except ApiError as exc:
        try:
            await status.delete()
        except TelegramBadRequest:
            pass
        if exc.status == 404:
            await message.answer(texts.QUEUE_EMPTY, reply_markup=keyboards.admin_reply_keyboard())
            return
        if exc.status == 400 and "состоян" in (exc.message or "").lower():
            await message.answer(texts.NOT_IN_MODERATION, reply_markup=keyboards.admin_reply_keyboard())
            return
        await message.answer(
            f"Не удалось загрузить жалобу: {exc.message}",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return
    except Exception:
        logger.exception("unexpected error loading ticket for %s", telegram_id)
        try:
            await status.delete()
        except TelegramBadRequest:
            pass
        await message.answer(
            "Не удалось загрузить жалобу. Попробуйте «📋 Текущая жалоба» или /start.",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    try:
        await status.delete()
    except TelegramBadRequest:
        pass

    view = _normalize_ticket(payload)
    remaining = view["remaining"]
    city = view["city"]
    photos = view["photos"]
    caption = texts.ticket_caption(view, remaining, city=city)
    actions = keyboards.moderation_actions()

    try:
        if not photos:
            await message.answer(
                caption + "\n\n⚠️ У профиля нет фотографий.",
                reply_markup=actions,
            )
        else:
            delivered = await _send_ticket_photos(
                message,
                caption=caption,
                photos=photos,
                actions=actions,
                main_bot_media=main_bot_media,
            )
            if not delivered:
                await message.answer(
                    caption + "\n\n⚠️ Не удалось загрузить фото профиля. Проверьте LOOKS_RATING_BOT_TOKEN / TELEGRAM_BOT_TOKEN.",
                    reply_markup=actions,
                )
    except TelegramBadRequest as exc:
        logger.warning("ticket media send failed for %s: %s", telegram_id, exc)
        await message.answer(
            caption + "\n\n⚠️ Telegram отклонил отправку фото. Попробуйте «📋 Обновить».",
            reply_markup=actions,
        )
    except Exception:
        logger.exception("failed to send ticket view for %s", telegram_id)
        await message.answer(
            caption + "\n\n⚠️ Ошибка отображения. Действия доступны кнопками под сообщением.",
            reply_markup=actions,
        )


async def _after_city_selected(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
    telegram_id: int,
    city: str,
    count: int,
) -> None:
    await state.set_state(ModerationStates.moderating)
    await message.answer(
        texts.CITY_SELECTED.format(city=city, count=count),
        reply_markup=keyboards.admin_reply_keyboard(),
    )
    await present_current_ticket(
        message,
        api,
        telegram_id,
        main_bot_media=main_bot_media,
        state=state,
    )


async def _advance_after_action(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
    telegram_id: int,
    notice: str,
) -> None:
    await present_current_ticket(
        message,
        api,
        telegram_id,
        main_bot_media=main_bot_media,
        state=state,
        notice=notice,
    )


async def _prompt_delete_profile(message: Message, state: FSMContext) -> None:
    await state.set_state(ModerationStates.confirming_delete_profile)
    await message.answer(
        texts.DELETE_PROFILE_CONFIRM,
        reply_markup=keyboards.delete_profile_confirmation(),
    )


async def _prompt_delete_account(message: Message, state: FSMContext) -> None:
    await state.set_state(ModerationStates.confirming_delete_account)
    await message.answer(
        texts.DELETE_ACCOUNT_CONFIRM,
        reply_markup=keyboards.delete_account_confirmation(),
    )


async def _cancel_delete(message: Message, state: FSMContext) -> None:
    await state.set_state(ModerationStates.moderating)
    await message.answer(texts.DELETE_CANCELLED, reply_markup=keyboards.admin_reply_keyboard())


async def _execute_delete_profile(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
    telegram_id: int,
) -> None:
    try:
        await api.delete_current(telegram_id)
    except ApiError as exc:
        await state.set_state(ModerationStates.moderating)
        await message.answer(f"Ошибка: {exc.message}", reply_markup=keyboards.admin_reply_keyboard())
        return
    await _advance_after_action(
        message, state, api, main_bot_media, telegram_id, texts.ACTION_DELETE_OK
    )


async def _execute_delete_account(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
    telegram_id: int,
) -> None:
    try:
        await api.delete_current_account(telegram_id)
    except ApiError as exc:
        await state.set_state(ModerationStates.moderating)
        await message.answer(f"Ошибка: {exc.message}", reply_markup=keyboards.admin_reply_keyboard())
        return
    await _advance_after_action(
        message, state, api, main_bot_media, telegram_id, texts.ACTION_DELETE_ACCOUNT_OK
    )


async def handle_moderation_panel_action(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
    text: str,
) -> None:
    telegram_id = message.from_user.id

    if text == keyboards.BTN_CITIES:
        from handlers.withdrawals import show_moderation_hub

        await show_moderation_hub(message, state)
        return

    if text == keyboards.BTN_CURRENT:
        await state.set_state(ModerationStates.moderating)
        await present_current_ticket(
            message,
            api,
            telegram_id,
            main_bot_media=main_bot_media,
            state=state,
        )
        return

    if text == keyboards.BTN_HELP:
        await state.set_state(ModerationStates.moderating)
        await message.answer(texts.MODERATION_HINT, reply_markup=keyboards.admin_reply_keyboard())
        return

    if text == keyboards.BTN_LOGOUT:
        await on_logout_reply(message, state, api)


@router.message(Command("menu", "help"))
async def on_menu_command(message: Message) -> None:
    await message.answer(texts.MODERATION_HINT, reply_markup=keyboards.admin_reply_keyboard())


@router.message(ModerationStates.confirming_delete_profile, F.text)
async def on_delete_profile_confirm_text(message: Message) -> None:
    await message.answer(
        "Подтвердите или отмените удаление кнопками в сообщении выше.",
        reply_markup=keyboards.admin_reply_keyboard(),
    )


@router.message(ModerationStates.confirming_delete_account, F.text)
async def on_delete_account_confirm_text(message: Message) -> None:
    await message.answer(
        "Подтвердите или отмените удаление кнопками в сообщении выше.",
        reply_markup=keyboards.admin_reply_keyboard(),
    )


@router.message(
    ModerationStates.selecting_city,
    F.text,
    ~F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_selecting_city_unknown_text(message: Message) -> None:
    await message.answer(
        "Выберите город кнопкой в сообщении выше или нажмите «🏙 Города».",
        reply_markup=keyboards.admin_reply_keyboard(),
    )


@router.message(
    ModerationStates.moderating,
    F.text,
    ~F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_moderating_unknown_text(message: Message) -> None:
    await message.answer(texts.UNKNOWN_COMMAND, reply_markup=keyboards.admin_reply_keyboard())


@router.callback_query(F.data == keyboards.CALLBACK_CHANGE_CITY)
async def on_change_city(callback: CallbackQuery, state: FSMContext) -> None:
    await callback.answer()
    if callback.message:
        from handlers.withdrawals import show_moderation_hub

        await show_moderation_hub(callback.message, state)


@router.callback_query(F.data == keyboards.CALLBACK_CURRENT)
async def on_current_callback(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    await callback.answer()
    if callback.message:
        await present_current_ticket(
            callback.message,
            api,
            callback.from_user.id,
            main_bot_media=main_bot_media,
            state=state,
        )


@router.callback_query(F.data == keyboards.CALLBACK_HELP)
async def on_help_callback(callback: CallbackQuery) -> None:
    await callback.answer()
    if callback.message:
        await callback.message.answer(texts.MODERATION_HINT, reply_markup=keyboards.admin_reply_keyboard())


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_CITY))
async def on_city_selected(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    await callback.answer()
    if not callback.message:
        return

    data = await state.get_data()
    cities: list[str] = list(data.get("cities") or [])
    raw_index = (callback.data or "").removeprefix(keyboards.CALLBACK_PREFIX_CITY)
    if not raw_index.isdigit():
        await callback.message.answer("Некорректный город")
        return

    index = int(raw_index)
    if index < 0 or index >= len(cities):
        await callback.message.answer("Список городов устарел, выберите снова.")
        await show_city_selection(callback.message, state, api, callback.from_user.id)
        return

    city = cities[index]
    try:
        result = await api.select_city(callback.from_user.id, city)
    except ApiError as exc:
        await callback.message.answer(
            f"Не удалось загрузить жалобы: {exc.message}",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    count = int(result.get("count", 0))
    if count <= 0:
        await state.set_state(ModerationStates.moderating)
        await callback.message.answer(texts.QUEUE_EMPTY, reply_markup=keyboards.admin_reply_keyboard())
        return

    await _after_city_selected(
        callback.message, state, api, main_bot_media, callback.from_user.id, city, count
    )


@router.callback_query(F.data == keyboards.CALLBACK_DISMISS)
async def on_dismiss_callback(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    await callback.answer()
    if not callback.message:
        return
    try:
        await api.dismiss_current(callback.from_user.id)
    except ApiError as exc:
        await callback.message.answer(f"Ошибка: {exc.message}", reply_markup=keyboards.admin_reply_keyboard())
        return
    await _advance_after_action(
        callback.message,
        state,
        api,
        main_bot_media,
        callback.from_user.id,
        texts.ACTION_DISMISS_OK,
    )


@router.callback_query(F.data == keyboards.CALLBACK_DELETE)
async def on_delete_callback(callback: CallbackQuery, state: FSMContext) -> None:
    await callback.answer()
    if callback.message:
        await _prompt_delete_profile(callback.message, state)


@router.callback_query(F.data == keyboards.CALLBACK_DELETE_ACCOUNT)
async def on_delete_account_callback(callback: CallbackQuery, state: FSMContext) -> None:
    await callback.answer()
    if callback.message:
        await _prompt_delete_account(callback.message, state)


@router.callback_query(F.data == keyboards.CALLBACK_DELETE_CANCEL)
async def on_delete_cancel(callback: CallbackQuery, state: FSMContext) -> None:
    await callback.answer("Отменено")
    if callback.message:
        await _cancel_delete(callback.message, state)


@router.callback_query(F.data == keyboards.CALLBACK_DELETE_CONFIRM)
async def on_delete_confirm(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    await callback.answer()
    if not callback.message:
        return
    await _execute_delete_profile(
        callback.message, state, api, main_bot_media, callback.from_user.id
    )


@router.callback_query(F.data == keyboards.CALLBACK_DELETE_ACCOUNT_CONFIRM)
async def on_delete_account_confirm(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    await callback.answer()
    if not callback.message:
        return
    await _execute_delete_account(
        callback.message, state, api, main_bot_media, callback.from_user.id
    )


@router.callback_query(F.data == keyboards.CALLBACK_SKIP)
async def on_skip_callback(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    await callback.answer()
    if not callback.message:
        return
    try:
        await api.skip_current(callback.from_user.id)
    except ApiError as exc:
        await callback.message.answer(f"Ошибка: {exc.message}", reply_markup=keyboards.admin_reply_keyboard())
        return
    await _advance_after_action(
        callback.message,
        state,
        api,
        main_bot_media,
        callback.from_user.id,
        texts.ACTION_SKIP_OK,
    )


async def on_logout_reply(message: Message, state: FSMContext, api: TicketApiClient) -> None:
    await state.clear()
    try:
        await api.logout(message.from_user.id)
    except ApiError as exc:
        logger.warning("logout failed for %s: %s", message.from_user.id, exc.message)
    await message.answer(texts.LOGOUT_OK, reply_markup=ReplyKeyboardRemove())
    await message.answer(texts.START_UNAUTH, reply_markup=keyboards.start_unauthenticated())
