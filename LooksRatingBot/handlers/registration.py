import logging

from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.keyboards import (
    MENU_CANCEL,
    BTN_DISPLAY_CUSTOM,
    BTN_DISPLAY_USE_TELEGRAM,
    cancel_keyboard,
    display_name_choice_keyboard,
    remove_keyboard,
)
from bot.services import (
    SessionState,
    format_api_error,
    send_main_menu,
    set_bot_state,
)
from bot.states import RegistrationStates
from handlers.photo import offer_photo_after_registration

logger = logging.getLogger(__name__)
router = Router()


async def begin_registration(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    telegram_id: int,
    username: str | None,
    referral_link: str | None = None,
) -> None:
    await state.update_data(username=username, referral_link=referral_link)
    try:
        await set_bot_state(api, telegram_id, SessionState.AWAITING_DISPLAY_NAME)
    except ApiError as exc:
        logger.error("Failed to set session state: %s", exc.message)
        await message.answer(format_api_error(exc))
        return

    if username:
        await state.set_state(RegistrationStates.display_choice)
        await message.answer(
            texts.REG_DISPLAY_TELEGRAM.format(username=username),
            reply_markup=display_name_choice_keyboard(),
        )
        return

    await ask_custom_display_name(message, state)


async def ask_custom_display_name(message: Message, state: FSMContext) -> None:
    await state.set_state(RegistrationStates.display_name)
    await message.answer(texts.WELCOME_NEW, reply_markup=cancel_keyboard())


@router.message(RegistrationStates.display_choice, F.text)
async def display_choice_entered(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text == MENU_CANCEL:
        await state.clear()
        await message.answer(texts.REG_CANCEL, reply_markup=remove_keyboard())
        return

    if message.text == BTN_DISPLAY_USE_TELEGRAM:
        await complete_registration(message, state, api, use_telegram_username_as_display=True)
        return

    if message.text == BTN_DISPLAY_CUSTOM:
        await ask_custom_display_name(message, state)
        return

    data = await state.get_data()
    username = data.get("username", "")
    await message.answer(
        texts.REG_DISPLAY_TELEGRAM.format(username=username),
        reply_markup=display_name_choice_keyboard(),
    )


@router.message(RegistrationStates.display_name, F.text)
async def display_name_entered(
    message: Message, state: FSMContext, api: LooksRatingApiClient
) -> None:
    if message.text == MENU_CANCEL:
        await state.clear()
        await message.answer(texts.REG_CANCEL, reply_markup=remove_keyboard())
        return

    display_name = message.text.strip()
    if not display_name or len(display_name) > 32:
        await message.answer(texts.REG_DISPLAY_NAME_INVALID, reply_markup=cancel_keyboard())
        return

    await complete_registration(
        message,
        state,
        api,
        use_telegram_username_as_display=False,
        display_name=display_name,
    )


async def complete_registration(
    message: Message,
    state: FSMContext,
    api: LooksRatingApiClient,
    *,
    use_telegram_username_as_display: bool,
    display_name: str | None = None,
) -> None:
    data = await state.get_data()
    username = data.get("username")
    telegram_id = message.from_user.id

    try:
        result = await api.register_user(
            telegram_id,
            username,
            use_telegram_username_as_display=use_telegram_username_as_display,
            display_name=display_name,
            referral_link=data.get("referral_link"),
        )
    except ApiError as exc:
        if exc.code == "UserAlreadyExists":
            await state.clear()
            await set_bot_state(api, telegram_id, SessionState.IDLE)
            await send_main_menu(message, api, telegram_id, texts.ALREADY_REGISTERED)
            return
        await message.answer(format_api_error(exc))
        return

    try:
        await set_bot_state(api, telegram_id, SessionState.REGISTERED)
    except ApiError as exc:
        logger.warning("set_bot_state after register failed for %s: %s", telegram_id, exc.message)
        await message.answer(format_api_error(exc))
        return

    await send_main_menu(
        message,
        api,
        telegram_id,
        texts.REG_DONE.format(display_name=result.get("displayName", "—")),
    )
    try:
        await offer_photo_after_registration(message, state, api, telegram_id)
    except ApiError as exc:
        logger.warning("offer_photo_after_registration failed for %s: %s", telegram_id, exc.message)
        await message.answer(texts.PHOTO_LATER, reply_markup=cancel_keyboard())
    except Exception:
        logger.exception("offer_photo_after_registration failed for %s", telegram_id)
        await message.answer(texts.PHOTO_LATER, reply_markup=cancel_keyboard())
