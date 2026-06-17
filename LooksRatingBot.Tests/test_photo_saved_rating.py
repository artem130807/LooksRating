from __future__ import annotations

from unittest.mock import AsyncMock, patch

import pytest

from bot.services import format_set_photo_saved_text
from bot.states import RecreatePhotoStates
from handlers.photo import photo_uploaded
from helpers.aiogram_builders import make_fsm_context, make_message
from helpers.fakes import FakeApiClient


class TestFormatSetPhotoSavedText:
    def test_vip_recreate_keeps_rating_count_in_message(self) -> None:
        text = format_set_photo_saved_text(
            {
                "city": "moscow",
                "rating": 7.5,
                "ratingCount": 10,
            }
        )

        assert "7.5/10" in text
        assert "10 оценок" in text

    def test_supports_pascal_case_api_fields(self) -> None:
        text = format_set_photo_saved_text(
            {
                "City": "spb",
                "Rating": 8.0,
                "RatingCount": 3,
            }
        )

        assert "8.0/10" in text
        assert "3 оценок" in text

    def test_reset_rating_shows_zero_count(self) -> None:
        text = format_set_photo_saved_text(
            {
                "city": "moscow",
                "rating": 0,
                "ratingCount": 0,
            }
        )

        assert "0.0/10" in text
        assert "0 оценок" in text


@pytest.mark.asyncio
class TestPhotoUploadSavedMessage:
    async def test_recreate_photo_uses_api_rating_count(self) -> None:
        api = FakeApiClient()
        api.set_photo_result = {
            "city": "moscow",
            "rating": 7.5,
            "ratingCount": 10,
        }
        state = await make_fsm_context(
            data={
                "recreate": True,
                "nomination": {
                    "useProfileNomination": False,
                    "city": "moscow",
                    "age": 25,
                    "gender": "Male",
                },
            }
        )
        await state.set_state(RecreatePhotoStates.upload)
        status = make_message()
        status.edit_text = AsyncMock()
        message = make_message(photo_file_id="telegram-file-42")
        message.answer = AsyncMock(return_value=status)

        with patch("handlers.photo._finish_photo_flow", new=AsyncMock()) as finish:
            await photo_uploaded(message, state, api)

        finish.assert_awaited_once()
        saved_text = finish.await_args.args[4]
        assert "7.5/10" in saved_text
        assert "10 оценок" in saved_text
        assert len(api.set_photo_calls) == 1
        assert api.set_photo_calls[0]["recreate"] is True
