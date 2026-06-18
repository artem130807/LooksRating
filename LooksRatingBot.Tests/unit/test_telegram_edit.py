import pytest
from unittest.mock import AsyncMock, MagicMock

from aiogram.exceptions import TelegramBadRequest
from bot.telegram_edit import edit_text_or_ignore_unchanged


@pytest.mark.asyncio
async def test_edit_text_or_ignore_unchanged_swallows_not_modified() -> None:
    message = MagicMock()
    message.from_user.id = 42_001
    message.edit_text = AsyncMock(
        side_effect=TelegramBadRequest(method=MagicMock(), message="message is not modified")
    )

    await edit_text_or_ignore_unchanged(message, "same text")

    message.edit_text.assert_awaited_once()


@pytest.mark.asyncio
async def test_edit_text_or_ignore_unchanged_reraises_other_bad_request() -> None:
    message = MagicMock()
    message.from_user.id = 42_001
    message.edit_text = AsyncMock(
        side_effect=TelegramBadRequest(method=MagicMock(), message="can't parse entities")
    )

    with pytest.raises(TelegramBadRequest):
        await edit_text_or_ignore_unchanged(message, "bad <html>")
