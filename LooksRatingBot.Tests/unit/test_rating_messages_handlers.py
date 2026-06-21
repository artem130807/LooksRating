from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from bot.states import RatingMessageStates, RatingStates
from handlers import rating_messages
from handlers.rating import _normalize_photo_payload
from helpers.aiogram_builders import make_callback, make_fsm_context, make_message
from services.in_memory_rating_user_message_store import InMemoryRatingUserMessageStore
from services.rating_user_message_service import RatingUserMessageService


@pytest.fixture
def rating_user_message_service() -> RatingUserMessageService:
    bot = MagicMock()
    return RatingUserMessageService(bot, InMemoryRatingUserMessageStore())


@pytest.mark.asyncio
async def test_normalize_photo_payload_reads_recipient_telegram_id() -> None:
    payload = _normalize_photo_payload(
        {
            "profileId": "profile-1",
            "recipientTelegramId": 77_001,
            "photos": [{"id": "photo-1", "telegramFileId": "file-1"}],
        }
    )

    assert payload["recipientTelegramId"] == 77_001


@pytest.mark.asyncio
async def test_rating_message_start_moves_to_compose_state() -> None:
    state = await make_fsm_context(
        data={
            "current_photo_id": "photo-1",
            "current_recipient_telegram_id": 20_001,
        }
    )
    callback = make_callback("msg:photo-1")

    await rating_messages.rating_message_start(callback, state)

    assert await state.get_state() == RatingMessageStates.compose
    callback.message.answer.assert_awaited_once()


@pytest.mark.asyncio
async def test_rating_message_compose_submit_sends_message_and_returns_to_rating(
    rating_user_message_service: RatingUserMessageService,
) -> None:
    state = await make_fsm_context(
        data={
            "current_photo_id": "photo-1",
            "message_photo_id": "photo-1",
            "current_recipient_telegram_id": 20_001,
            "current_file_id": "file-1",
            "current_profile_id": "profile-1",
            "current_caption": "caption",
            "current_photos": [{"id": "photo-1", "telegramFileId": "file-1"}],
        }
    )
    await state.set_state(RatingMessageStates.compose)

    message = make_message("Привет!")
    api = MagicMock()
    rating_user_message_service.send_message = AsyncMock(return_value=(True, "Сообщение отправлено."))

    with patch(
        "handlers.rating_messages._resend_current_photo",
        new=AsyncMock(),
    ) as resend:
        await rating_messages.rating_message_compose_submit(
            message,
            state,
            api,
            rating_user_message_service,
        )

    rating_user_message_service.send_message.assert_awaited_once()
    assert await state.get_state() == RatingStates.awaiting_rating
    resend.assert_awaited_once()


@pytest.mark.asyncio
async def test_rating_message_show_reveals_body(
    rating_user_message_service: RatingUserMessageService,
) -> None:
    saved = await rating_user_message_service.store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Иван",
        text="Текст",
    )
    callback = make_callback(f"rms:sh:{saved.token}", user_id=20_001)

    await rating_messages.rating_message_show(callback, rating_user_message_service)

    callback.message.edit_text.assert_awaited_once()
    edited_text = callback.message.edit_text.await_args.args[0]
    assert "Иван" in edited_text
    assert "Текст" in edited_text


@pytest.mark.asyncio
async def test_rating_message_reply_start_sets_reply_state(
    rating_user_message_service: RatingUserMessageService,
) -> None:
    saved = await rating_user_message_service.store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Иван",
        text="Текст",
    )
    state = await make_fsm_context()
    callback = make_callback(f"rms:rp:{saved.token}", user_id=20_001)

    await rating_messages.rating_message_reply_start(callback, state, rating_user_message_service)

    assert await state.get_state() == RatingMessageStates.reply_compose
    data = await state.get_data()
    assert data["reply_recipient_telegram_id"] == 10_001


@pytest.mark.asyncio
async def test_rating_message_show_rejects_non_recipient(
    rating_user_message_service: RatingUserMessageService,
) -> None:
    saved = await rating_user_message_service.store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="Иван",
        text="Текст",
    )
    callback = make_callback(f"rms:sh:{saved.token}", user_id=99_999)

    await rating_messages.rating_message_show(callback, rating_user_message_service)

    callback.message.edit_text.assert_not_called()
    callback.answer.assert_awaited_once()


@pytest.mark.asyncio
async def test_rating_message_show_escapes_html(
    rating_user_message_service: RatingUserMessageService,
) -> None:
    saved = await rating_user_message_service.store.save(
        recipient_telegram_id=20_001,
        sender_telegram_id=10_001,
        sender_display_name="<b>Иван</b>",
        text="<script>alert(1)</script>",
    )
    callback = make_callback(f"rms:sh:{saved.token}", user_id=20_001)

    await rating_messages.rating_message_show(callback, rating_user_message_service)

    edited_text = callback.message.edit_text.await_args.args[0]
    assert "&lt;b&gt;Иван&lt;/b&gt;" in edited_text
    assert "<script>" not in edited_text
