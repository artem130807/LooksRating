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
    video_file_id: str | None = None,
    video_note_file_id: str | None = None,
    animation_file_id: str | None = None,
    document_mime: str | None = None,
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

    if video_file_id is not None:
        video = MagicMock()
        video.file_id = video_file_id
        message.video = video
    else:
        message.video = None

    if video_note_file_id is not None:
        video_note = MagicMock()
        video_note.file_id = video_note_file_id
        message.video_note = video_note
    else:
        message.video_note = None

    if animation_file_id is not None:
        animation = MagicMock()
        animation.file_id = animation_file_id
        message.animation = animation
    else:
        message.animation = None

    if document_mime is not None:
        document = MagicMock()
        document.mime_type = document_mime
        document.file_id = "document-file"
        message.document = document
    else:
        message.document = None

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
