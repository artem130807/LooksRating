from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import ApiError, TicketApiClient
from bot.states import AuthStates
from handlers.common import process_login_text, process_password_text

router = Router()


@router.message(AuthStates.awaiting_login, F.text)
async def on_login(message: Message, state: FSMContext, api: TicketApiClient) -> None:
    try:
        await process_login_text(message, state, api)
    except ApiError as exc:
        await message.answer(f"Ошибка API: {exc.message}")


@router.message(AuthStates.awaiting_password, F.text)
async def on_password(message: Message, state: FSMContext, api: TicketApiClient) -> None:
    try:
        await process_password_text(message, state, api)
    except ApiError as exc:
        if exc.status == 429:
            await message.answer("Слишком много попыток входа. Подождите минуту и попробуйте снова.")
            return
        await message.answer(f"Ошибка входа: {exc.message}")
