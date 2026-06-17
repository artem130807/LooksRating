from __future__ import annotations

from aiogram import F
from aiogram.filters import StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import Message

from bot.keyboards import cancel_keyboard, multi_photo_upload_keyboard
from bot.states import PhotoStates, RecreatePhotoStates

PHOTO_UPLOAD_STATE = StateFilter(
    PhotoStates.upload,
    RecreatePhotoStates.upload,
    RecreatePhotoStates.upload_many,
)

VIDEO_UPLOAD = F.video | F.video_note | F.animation


def is_video_document(message: Message) -> bool:
    document = message.document
    if document is None or not document.mime_type:
        return False
    return document.mime_type.lower().startswith("video/")


async def reply_photo_upload_required(
    message: Message,
    state: FSMContext,
    *,
    text: str,
) -> None:
    current = await state.get_state()
    markup = (
        multi_photo_upload_keyboard()
        if current == RecreatePhotoStates.upload_many.state
        else cancel_keyboard()
    )
    await message.answer(text, reply_markup=markup)
