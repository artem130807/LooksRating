"""Единая точка входа для reply-клавиатуры админ-панели (модерация + мониторинг)."""

from aiogram import F, Router
from aiogram.filters import StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from api.client import TicketApiClient
from bot import keyboards
from bot.session_sync import is_authenticated
from bot.states import ModerationStates, OpsStates
from bot.telegram_media import MainBotMediaService
from handlers.common import load_session
from handlers.moderation import handle_moderation_panel_action
from handlers.monitoring import _stop_log_refresh, handle_ops_entry

router = Router()

# Все состояния, в которых админ может пользоваться нижней панелью.
ADMIN_PANEL_STATES = (
    ModerationStates.selecting_moderation_type,
    ModerationStates.selecting_city,
    ModerationStates.selecting_withdrawal_city,
    ModerationStates.viewing_withdrawal_list,
    ModerationStates.viewing_withdrawal_detail,
    ModerationStates.moderating,
    ModerationStates.confirming_delete_profile,
    ModerationStates.confirming_delete_account,
    OpsStates.viewing_hub,
    OpsStates.viewing_logs,
    None,
)

_CONFIRM_STATES = frozenset(
    {
        ModerationStates.confirming_delete_profile.state,
        ModerationStates.confirming_delete_account.state,
    }
)


async def _require_authenticated(message: Message, api: TicketApiClient) -> bool:
    session = await load_session(api, message.from_user.id)
    return is_authenticated(session)


@router.message(
    StateFilter(*ADMIN_PANEL_STATES),
    F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_admin_panel_reply(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    main_bot_media: MainBotMediaService,
) -> None:
    if not await _require_authenticated(message, api):
        return

    await _stop_log_refresh(message.chat.id)

    current = await state.get_state()
    if current in _CONFIRM_STATES:
        await state.set_state(ModerationStates.moderating)

    text = (message.text or "").strip()
    if text == keyboards.BTN_OPS:
        await handle_ops_entry(message, state, api)
        return

    await handle_moderation_panel_action(
        message,
        state,
        api,
        main_bot_media,
        text,
    )
