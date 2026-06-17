from __future__ import annotations

import pytest

from bot.services import SessionState
from bot.session_sync import (
    extract_session_state,
    is_feed_setup_session,
    is_rating_session,
    restore_fsm_from_api,
)
from bot.states import FeedSetupStates, PhotoStates, RatingStates, RegistrationStates
from helpers.aiogram_builders import make_fsm_context
from helpers.fakes import FakeApiClient


class TestExtractSessionState:
    def test_reads_camel_and_pascal_case(self) -> None:
        assert extract_session_state({"state": "Idle"}) == "Idle"
        assert extract_session_state({"State": "Rating"}) == "Rating"

    def test_empty_session_returns_none(self) -> None:
        assert extract_session_state(None) is None
        assert extract_session_state({}) is None


class TestSessionPredicates:
    def test_rating_and_feed_flags(self) -> None:
        assert is_rating_session(SessionState.RATING) is True
        assert is_feed_setup_session(SessionState.AWAITING_FEED_CITY) is True
        assert is_feed_setup_session(SessionState.IDLE) is False


@pytest.mark.asyncio
class TestRestoreFsmFromApi:
    async def test_awaiting_photo_does_not_restore_fsm(self) -> None:
        """Regression: mapping AwaitingPhoto → confirm_create broke nomination upload."""
        api = FakeApiClient(session={"state": SessionState.AWAITING_PHOTO})
        state = await make_fsm_context()

        restored = await restore_fsm_from_api(state, api, 42_001)

        assert restored == SessionState.AWAITING_PHOTO
        assert await state.get_state() is None

    async def test_awaiting_feed_city_restores_fsm_and_loads_cities(self) -> None:
        api = FakeApiClient(session={"state": SessionState.AWAITING_FEED_CITY})
        state = await make_fsm_context()

        restored = await restore_fsm_from_api(state, api, 42_001)

        assert restored == SessionState.AWAITING_FEED_CITY
        assert await state.get_state() == FeedSetupStates.city.state
        data = await state.get_data()
        assert data["cities"] == api.cities
        assert data["feed_setup"] is True

    async def test_awaiting_display_name_with_username_goes_to_display_choice(self) -> None:
        api = FakeApiClient(
            session={
                "state": SessionState.AWAITING_DISPLAY_NAME,
                "telegramUsername": "cool_user",
            }
        )
        state = await make_fsm_context()

        await restore_fsm_from_api(state, api, 42_001)

        assert await state.get_state() == RegistrationStates.display_choice.state
        assert (await state.get_data())["username"] == "cool_user"

    async def test_rating_session_restores_rating_fsm(self) -> None:
        api = FakeApiClient(session={"state": SessionState.RATING})
        state = await make_fsm_context()

        await restore_fsm_from_api(state, api, 42_001)

        assert await state.get_state() == RatingStates.awaiting_rating.state

    async def test_does_not_override_existing_fsm(self) -> None:
        api = FakeApiClient(session={"state": SessionState.RATING})
        state = await make_fsm_context()
        await state.set_state(PhotoStates.upload)

        restored = await restore_fsm_from_api(state, api, 42_001)

        assert restored is None
        assert await state.get_state() == PhotoStates.upload.state

    async def test_awaiting_display_name_without_username_goes_to_display_name(self) -> None:
        api = FakeApiClient(session={"state": SessionState.AWAITING_DISPLAY_NAME})
        state = await make_fsm_context()

        await restore_fsm_from_api(state, api, 42_001)

        assert await state.get_state() == RegistrationStates.display_name.state

    async def test_idle_session_is_not_restored(self) -> None:
        api = FakeApiClient(session={"state": SessionState.IDLE})
        state = await make_fsm_context()

        restored = await restore_fsm_from_api(state, api, 42_001)

        assert restored == SessionState.IDLE
        assert await state.get_state() is None
