from __future__ import annotations

from aiogram.types import CallbackQuery, Message

from api.client import TicketApiClient
from bot import keyboards
from bot.session_sync import is_authenticated
from handlers.common import load_session


async def require_authenticated_callback(callback: CallbackQuery, api: TicketApiClient) -> bool:
    session = await load_session(api, callback.from_user.id)
    if is_authenticated(session):
        return True
    if callback.message:
        await callback.message.answer(
            "Сначала войдите в аккаунт администратора.",
            reply_markup=keyboards.start_unauthenticated(),
        )
    else:
        await callback.answer("Требуется вход администратора", show_alert=True)
    return False
