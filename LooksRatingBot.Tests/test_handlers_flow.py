from __future__ import annotations

from unittest.mock import AsyncMock, patch

import pytest

from bot import texts
from bot.services import SessionState
from bot.states import FeedSetupStates
from handlers.feed_setup import begin_feed_setup, feed_city_entered
from handlers.session_recovery import answer_orphan_session_hint
from handlers.start_logic import handle_start
from helpers.aiogram_builders import make_fsm_context, make_message
from helpers.fakes import FakeApiClient


@pytest.mark.asyncio
class TestStartLogic:
    async def test_start_existing_user_opens_main_menu(self) -> None:
        api = FakeApiClient(user={"telegramId": 42_001, "hasRecommendationSettings": True})
        state = await make_fsm_context()
        message = make_message("/start")

        with patch("handlers.start_logic.send_main_menu", new=AsyncMock()) as menu:
            await handle_start(message, state, api)

        menu.assert_awaited_once()
        assert menu.await_args.args[3] == texts.WELCOME_BACK
        assert SessionState.IDLE in api.session_states

    async def test_start_new_user_begins_registration(self) -> None:
        api = FakeApiClient(user=None)
        state = await make_fsm_context()
        message = make_message("/start", username="newbie")

        with patch("handlers.start_logic.begin_registration", new=AsyncMock()) as begin:
            await handle_start(message, state, api)

        begin.assert_awaited_once()
        assert begin.await_args.args[3] == 42_001
        assert begin.await_args.args[4] == "newbie"


@pytest.mark.asyncio
class TestFeedSetup:
    async def test_begin_feed_setup_sets_city_state_and_api_session(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context()
        message = make_message()

        await begin_feed_setup(message, state, api, 42_001)

        assert await state.get_state() == FeedSetupStates.city.state
        assert SessionState.AWAITING_FEED_CITY in api.session_states
        data = await state.get_data()
        assert data["cities"] == api.cities
        message.answer.assert_awaited_once()

    async def test_feed_city_valid_input_advances_to_age(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"cities": api.cities, "feed_setup": True})
        await state.set_state(FeedSetupStates.city)

        await feed_city_entered(make_message("Moscow"), state, api)

        assert await state.get_state() == FeedSetupStates.age.state
        assert (await state.get_data())["city"] == "moscow"


@pytest.mark.asyncio
class TestSessionRecovery:
    async def test_orphan_rating_session_shows_hint(self) -> None:
        api = FakeApiClient(session={"state": SessionState.RATING})
        message = make_message()

        handled = await answer_orphan_session_hint(message, api, 42_001)

        assert handled is True
        message.answer.assert_awaited_once()

    async def test_orphan_feed_setup_session_shows_hint(self) -> None:
        api = FakeApiClient(session={"state": SessionState.AWAITING_FEED_AGE})
        message = make_message()

        handled = await answer_orphan_session_hint(message, api, 42_001)

        assert handled is True
        message.answer.assert_awaited_once()
        hint = message.answer.await_args.args[0]
        assert "лент" in hint.lower()

    async def test_orphan_idle_session_returns_false(self) -> None:
        api = FakeApiClient(session={"state": SessionState.IDLE})
        message = make_message()

        handled = await answer_orphan_session_hint(message, api, 42_001)

        assert handled is False
        message.answer.assert_not_awaited()
