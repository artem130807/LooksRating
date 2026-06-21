from __future__ import annotations

from unittest.mock import AsyncMock, MagicMock

import pytest

from bot import texts
from bot.writing_off_sparks_user_notifier import WritingOffSparksUserNotifier


@pytest.mark.asyncio
async def test_notify_confirmed_sends_message() -> None:
    bot = MagicMock()
    bot.send_message = AsyncMock()
    notifier = WritingOffSparksUserNotifier(bot)

    delivered = await notifier.notify_confirmed(telegram_id=10001, stars=200)

    assert delivered is True
    bot.send_message.assert_awaited_once_with(
        chat_id=10001,
        text=texts.WRITING_OFF_SPARKS_STARS_CREDITED.format(stars=200),
    )


@pytest.mark.asyncio
async def test_notify_cancelled_sends_message() -> None:
    bot = MagicMock()
    bot.send_message = AsyncMock()
    notifier = WritingOffSparksUserNotifier(bot)

    delivered = await notifier.notify_cancelled(
        telegram_id=10002,
        stars=100,
        sparks=1200,
    )

    assert delivered is True
    bot.send_message.assert_awaited_once_with(
        chat_id=10002,
        text=texts.WRITING_OFF_SPARKS_WITHDRAWAL_CANCELLED.format(stars=100, sparks=1200),
    )
