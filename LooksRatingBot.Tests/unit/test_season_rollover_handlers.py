from __future__ import annotations

from unittest.mock import AsyncMock, patch

import pytest

from bot.keyboards import season_rollover_notification_keyboard
from handlers import season_rollover
from helpers.aiogram_builders import make_callback, make_fsm_context


def test_season_rollover_keyboard_contains_expected_callbacks() -> None:
    keyboard = season_rollover_notification_keyboard("season-closed-1")
    callbacks = [
        button.callback_data
        for row in keyboard.inline_keyboard
        for button in row
    ]

    assert callbacks == [
        "season_rollover:results:season-closed-1",
        "season_rollover:add_photo",
    ]


@pytest.mark.asyncio
async def test_season_rollover_results_opens_gender_pick() -> None:
    callback = make_callback(
        user_id=42,
        data="season_rollover:results:season-123",
    )
    api = AsyncMock()
    api.get_user.return_value = {"hasRecommendationSettings": True, "age": 25}

    with patch(
        "handlers.season_rollover._send_gender_pick",
        new_callable=AsyncMock,
    ) as gender_pick:
        await season_rollover.season_rollover_results(callback, api)

    gender_pick.assert_awaited_once()
    assert gender_pick.await_args.kwargs["season_id"] == "season-123"
    assert gender_pick.await_args.kwargs["scope"] == "season"


@pytest.mark.asyncio
async def test_season_rollover_add_photo_starts_nomination_flow() -> None:
    callback = make_callback(user_id=42, data="season_rollover:add_photo")
    state = await make_fsm_context()
    api = AsyncMock()

    with patch(
        "handlers.season_rollover.start_nomination_flow",
        new_callable=AsyncMock,
    ) as nomination_flow:
        await season_rollover.season_rollover_add_photo(callback, state, api)

    nomination_flow.assert_awaited_once()
    assert nomination_flow.await_args.kwargs["recreate"] is False
    assert nomination_flow.await_args.kwargs["from_settings"] is False
