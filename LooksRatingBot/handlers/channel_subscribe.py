from __future__ import annotations

import logging

from aiogram import F, Router
from aiogram.types import CallbackQuery

from api.grpc_clients import LooksRatingGrpcClient
from bot import callbacks, texts
from bot.telegram_edit import edit_text_or_ignore_unchanged
from config import Settings
from services.channel_subscribe import process_subscribe_confirm

router = Router()
logger = logging.getLogger(__name__)


@router.callback_query(F.data == callbacks.CHANNEL_SUBSCRIBE_CONFIRM)
async def on_channel_subscribe_confirm(
    callback: CallbackQuery,
    settings: Settings,
    grpc: LooksRatingGrpcClient,
) -> None:
    if callback.from_user is None or callback.message is None:
        await callback.answer()
        return

    await callback.answer()

    try:
        message_text = await process_subscribe_confirm(
            grpc,
            callback.bot,
            callback.from_user.id,
            settings.channel_username,
        )
    except Exception:
        logger.exception(
            "Channel subscribe confirm failed for telegram_id=%s",
            callback.from_user.id,
        )
        message_text = texts.CHANNEL_SUBSCRIBE_FAILED

    await edit_text_or_ignore_unchanged(
        callback.message,
        message_text,
        reply_markup=None,
    )


@router.callback_query(F.data == callbacks.CHANNEL_SUBSCRIBE_SKIP)
async def on_channel_subscribe_skip(callback: CallbackQuery) -> None:
    if callback.message is None:
        await callback.answer()
        return

    await callback.answer()
    await edit_text_or_ignore_unchanged(
        callback.message,
        texts.CHANNEL_SUBSCRIBE_SKIPPED,
        reply_markup=None,
    )
