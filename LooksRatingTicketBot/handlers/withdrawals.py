from __future__ import annotations

import asyncio
import logging

from aiogram import Bot, F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from api.client import TicketApiClient
from api.main_bot_notify_client import MainBotNotifyClient
from api.writing_off_sparks_client import WritingOffSparksGrpcClient
from bot import keyboards, texts
from bot.states import ModerationStates
from handlers.admin_auth import require_authenticated_callback
from bot.withdrawal_views import (
    OUTPUT_STATUS_PENDING,
    WITHDRAWAL_PAGE_SIZE,
    format_withdrawal_detail,
    format_withdrawal_list_header,
)

router = Router()
logger = logging.getLogger(__name__)


async def show_moderation_hub(message: Message, state: FSMContext) -> None:
    await state.set_state(ModerationStates.selecting_moderation_type)
    await message.answer(
        texts.MODERATION_HUB_INTRO,
        reply_markup=keyboards.admin_reply_keyboard(),
    )
    await message.answer(
        "Выберите раздел:",
        reply_markup=keyboards.moderation_hub(),
    )


async def show_withdrawal_city_selection(
    message: Message,
    state: FSMContext,
    client: WritingOffSparksGrpcClient,
) -> None:
    success, error_message, cities = await asyncio.to_thread(client.list_pending_cities)
    if not success:
        await message.answer(
            error_message or "Не удалось загрузить города",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    if not cities:
        await state.set_state(ModerationStates.moderating)
        await message.answer(
            texts.NO_WITHDRAWAL_CITIES,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    await state.set_state(ModerationStates.selecting_withdrawal_city)
    await state.update_data(cities=cities)
    await message.answer(
        texts.WITHDRAWAL_CITY_PROMPT,
        reply_markup=keyboards.admin_reply_keyboard(),
    )
    await message.answer(
        "Список городов:",
        reply_markup=keyboards.withdrawal_city_selection(cities),
    )


async def _resolve_username(bot: Bot, telegram_id: int) -> str | None:
    try:
        chat = await bot.get_chat(telegram_id)
    except Exception:
        logger.debug("could not resolve username for %s", telegram_id, exc_info=True)
        return None
    if chat.username:
        return f"@{chat.username}"
    return None


async def present_withdrawal_list(
    message: Message,
    state: FSMContext,
    client: WritingOffSparksGrpcClient,
    *,
    city: str,
    page: int = 1,
) -> None:
    response = await asyncio.to_thread(
        client.list_by_city,
        city,
        page,
        WITHDRAWAL_PAGE_SIZE,
    )
    if not response.success:
        await message.answer(
            response.message or "Не удалось загрузить заявки",
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    await state.set_state(ModerationStates.viewing_withdrawal_list)
    await state.update_data(
        withdrawal_city=city,
        withdrawal_page=response.page,
    )

    if response.total_count == 0:
        await message.answer(
            texts.WITHDRAWAL_QUEUE_EMPTY.format(city=city),
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    header = format_withdrawal_list_header(
        city=city,
        page=response.page,
        page_size=response.page_size,
        total_count=response.total_count,
    )
    await message.answer(
        header,
        reply_markup=keyboards.withdrawal_list_keyboard(
            response.items,
            page=response.page,
            has_next_page=response.has_next_page,
        ),
    )


async def present_withdrawal_detail(
    message: Message,
    state: FSMContext,
    client: WritingOffSparksGrpcClient,
    bot: Bot,
    request_id: str,
) -> None:
    response = await asyncio.to_thread(client.get_by_id, request_id)
    if not response.success or response.item is None:
        await message.answer(
            response.message or texts.WITHDRAWAL_NOT_FOUND,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    username = await _resolve_username(bot, response.item.telegram_id)
    caption = format_withdrawal_detail(response.item, username=username)
    is_pending = response.item.status == OUTPUT_STATUS_PENDING

    await state.set_state(ModerationStates.viewing_withdrawal_detail)
    await state.update_data(withdrawal_request_id=request_id)
    await message.answer(
        caption,
        reply_markup=keyboards.withdrawal_detail_actions(request_id, allow_status_change=is_pending),
        disable_web_page_preview=True,
    )


@router.callback_query(F.data == keyboards.CALLBACK_MOD_HUB_COMPLAINTS)
async def on_hub_complaints(
    callback: CallbackQuery,
    state: FSMContext,
    api,
) -> None:
    await callback.answer()
    if callback.message:
        from handlers.moderation import show_city_selection

        await show_city_selection(callback.message, state, api, callback.from_user.id)


@router.callback_query(F.data == keyboards.CALLBACK_MOD_HUB_WITHDRAWALS)
async def on_hub_withdrawals(
    callback: CallbackQuery,
    state: FSMContext,
    writing_off_sparks: WritingOffSparksGrpcClient,
) -> None:
    await callback.answer()
    if callback.message:
        await show_withdrawal_city_selection(callback.message, state, writing_off_sparks)


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_WITHDRAWAL_CITY))
async def on_withdrawal_city_selected(
    callback: CallbackQuery,
    state: FSMContext,
    writing_off_sparks: WritingOffSparksGrpcClient,
) -> None:
    await callback.answer()
    if not callback.message:
        return

    data = await state.get_data()
    cities: list[str] = list(data.get("cities") or [])
    raw_index = (callback.data or "").removeprefix(keyboards.CALLBACK_PREFIX_WITHDRAWAL_CITY)
    if not raw_index.isdigit():
        await callback.message.answer("Некорректный город")
        return

    index = int(raw_index)
    if index < 0 or index >= len(cities):
        await callback.message.answer("Список городов устарел, выберите снова.")
        return

    city = cities[index]
    await present_withdrawal_list(
        callback.message,
        state,
        writing_off_sparks,
        city=city,
        page=1,
    )


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_WITHDRAWAL_PAGE))
async def on_withdrawal_page(
    callback: CallbackQuery,
    state: FSMContext,
    writing_off_sparks: WritingOffSparksGrpcClient,
) -> None:
    await callback.answer()
    if not callback.message:
        return

    data = await state.get_data()
    city = str(data.get("withdrawal_city") or "")
    if not city:
        await callback.message.answer(texts.WITHDRAWAL_CONTEXT_LOST)
        return

    raw_page = (callback.data or "").removeprefix(keyboards.CALLBACK_PREFIX_WITHDRAWAL_PAGE)
    if not raw_page.isdigit():
        return

    await present_withdrawal_list(
        callback.message,
        state,
        writing_off_sparks,
        city=city,
        page=int(raw_page),
    )


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_WITHDRAWAL_OPEN))
async def on_withdrawal_open(
    callback: CallbackQuery,
    state: FSMContext,
    writing_off_sparks: WritingOffSparksGrpcClient,
    bot: Bot,
) -> None:
    await callback.answer()
    if not callback.message:
        return

    request_id = (callback.data or "").removeprefix(keyboards.CALLBACK_PREFIX_WITHDRAWAL_OPEN)
    if not request_id:
        return

    await present_withdrawal_detail(
        callback.message,
        state,
        writing_off_sparks,
        bot,
        request_id,
    )


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_WITHDRAWAL_EXIT))
async def on_withdrawal_exit(
    callback: CallbackQuery,
    state: FSMContext,
    writing_off_sparks: WritingOffSparksGrpcClient,
) -> None:
    await callback.answer()
    if not callback.message:
        return

    data = await state.get_data()
    city = str(data.get("withdrawal_city") or "")
    page = int(data.get("withdrawal_page") or 1)
    if not city:
        await callback.message.answer(texts.WITHDRAWAL_CONTEXT_LOST)
        return

    await present_withdrawal_list(
        callback.message,
        state,
        writing_off_sparks,
        city=city,
        page=page,
    )


async def _refresh_list_after_status_change(
    callback: CallbackQuery,
    state: FSMContext,
    client: WritingOffSparksGrpcClient,
    *,
    notice: str,
) -> None:
    data = await state.get_data()
    city = str(data.get("withdrawal_city") or "")
    page = int(data.get("withdrawal_page") or 1)
    await callback.message.answer(notice)
    if city:
        await present_withdrawal_list(
            callback.message,
            state,
            client,
            city=city,
            page=page,
        )


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_WITHDRAWAL_DONE))
async def on_withdrawal_done(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    writing_off_sparks: WritingOffSparksGrpcClient,
    main_bot_notify: MainBotNotifyClient,
) -> None:
    await callback.answer()
    if not callback.message:
        return
    if not await require_authenticated_callback(callback, api):
        return

    request_id = (callback.data or "").removeprefix(keyboards.CALLBACK_PREFIX_WITHDRAWAL_DONE)
    detail = await asyncio.to_thread(writing_off_sparks.get_by_id, request_id)
    if not detail.success or detail.item is None:
        await callback.message.answer(
            detail.message or texts.WITHDRAWAL_NOT_FOUND,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    result = await asyncio.to_thread(writing_off_sparks.mark_confirmed, request_id)
    if not result.success:
        await callback.message.answer(
            result.message or texts.WITHDRAWAL_STATUS_UPDATE_FAILED,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    notify_result = await main_bot_notify.notify_writing_off_sparks_confirmed(
        telegram_id=detail.item.telegram_id,
        stars=detail.item.stars,
    )
    if not notify_result.success:
        logger.warning(
            "Main bot notify failed for withdrawal %s user %s: %s",
            request_id,
            detail.item.telegram_id,
            notify_result.message,
        )

    await _refresh_list_after_status_change(
        callback,
        state,
        writing_off_sparks,
        notice=texts.WITHDRAWAL_MARKED_CONFIRMED,
    )


@router.callback_query(F.data.startswith(keyboards.CALLBACK_PREFIX_WITHDRAWAL_CANCEL))
async def on_withdrawal_cancel(
    callback: CallbackQuery,
    state: FSMContext,
    api: TicketApiClient,
    writing_off_sparks: WritingOffSparksGrpcClient,
    main_bot_notify: MainBotNotifyClient,
) -> None:
    await callback.answer()
    if not callback.message:
        return
    if not await require_authenticated_callback(callback, api):
        return

    request_id = (callback.data or "").removeprefix(keyboards.CALLBACK_PREFIX_WITHDRAWAL_CANCEL)
    detail = await asyncio.to_thread(writing_off_sparks.get_by_id, request_id)
    if not detail.success or detail.item is None:
        await callback.message.answer(
            detail.message or texts.WITHDRAWAL_NOT_FOUND,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    result = await asyncio.to_thread(writing_off_sparks.mark_cancelled, request_id)
    if not result.success:
        await callback.message.answer(
            result.message or texts.WITHDRAWAL_STATUS_UPDATE_FAILED,
            reply_markup=keyboards.admin_reply_keyboard(),
        )
        return

    notify_result = await main_bot_notify.notify_writing_off_sparks_cancelled(
        telegram_id=detail.item.telegram_id,
        stars=detail.item.stars,
        sparks_count=detail.item.sparks_count,
    )
    if not notify_result.success:
        logger.warning(
            "Main bot notify failed for cancelled withdrawal %s user %s: %s",
            request_id,
            detail.item.telegram_id,
            notify_result.message,
        )

    await _refresh_list_after_status_change(
        callback,
        state,
        writing_off_sparks,
        notice=texts.WITHDRAWAL_MARKED_CANCELLED,
    )


@router.message(
    ModerationStates.selecting_moderation_type,
    F.text,
    ~F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_selecting_moderation_type_unknown(message: Message) -> None:
    await message.answer(
        texts.MODERATION_HUB_HINT,
        reply_markup=keyboards.admin_reply_keyboard(),
    )


@router.message(
    ModerationStates.selecting_withdrawal_city,
    F.text,
    ~F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_selecting_withdrawal_city_unknown(message: Message) -> None:
    await message.answer(
        texts.WITHDRAWAL_CITY_HINT,
        reply_markup=keyboards.admin_reply_keyboard(),
    )


@router.message(
    ModerationStates.viewing_withdrawal_list,
    F.text,
    ~F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_viewing_withdrawal_list_unknown(message: Message) -> None:
    await message.answer(
        texts.WITHDRAWAL_LIST_HINT,
        reply_markup=keyboards.admin_reply_keyboard(),
    )


@router.message(
    ModerationStates.viewing_withdrawal_detail,
    F.text,
    ~F.text.in_(keyboards.ADMIN_PANEL_BUTTONS),
)
async def on_viewing_withdrawal_detail_unknown(message: Message) -> None:
    await message.answer(
        texts.WITHDRAWAL_DETAIL_HINT,
        reply_markup=keyboards.admin_reply_keyboard(),
    )
