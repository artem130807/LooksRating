from __future__ import annotations

from typing import Any
from unittest.mock import AsyncMock, MagicMock

from aiogram.fsm.context import FSMContext
from aiogram.fsm.storage.base import StorageKey
from aiogram.fsm.storage.memory import MemoryStorage


def make_user(*, user_id: int = 42_001, username: str | None = "test_user") -> MagicMock:
    user = MagicMock()
    user.id = user_id
    user.username = username
    return user


def make_message(
    text: str | None = None,
    *,
    user_id: int = 42_001,
    username: str | None = "test_user",
    photo_file_id: str | None = None,
    media_group_id: str | None = None,
) -> MagicMock:
    message = MagicMock()
    message.text = text
    message.from_user = make_user(user_id=user_id, username=username)
    message.answer = AsyncMock()
    message.edit_text = AsyncMock()
    message.delete = AsyncMock()
    message.media_group_id = media_group_id

    if photo_file_id is not None:
        photo = MagicMock()
        photo.file_id = photo_file_id
        message.photo = [photo]
    else:
        message.photo = None

    return message


def make_callback(
    data: str,
    *,
    user_id: int = 42_001,
    username: str | None = "test_user",
) -> MagicMock:
    callback = MagicMock()
    callback.data = data
    callback.from_user = make_user(user_id=user_id, username=username)
    callback.message = make_message(user_id=user_id, username=username)
    callback.answer = AsyncMock()
    return callback


async def make_fsm_context(
    *,
    user_id: int = 42_001,
    chat_id: int = 42_001,
    data: dict[str, Any] | None = None,
) -> FSMContext:
    storage = MemoryStorage()
    key = StorageKey(bot_id=1, chat_id=chat_id, user_id=user_id)
    context = FSMContext(storage=storage, key=key)
    if data:
        await context.update_data(**data)
    return context
