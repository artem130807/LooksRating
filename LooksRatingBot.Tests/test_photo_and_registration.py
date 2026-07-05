from __future__ import annotations

from unittest.mock import AsyncMock, patch

import pytest
from api.client import ApiError

from bot.keyboards import BTN_DISPLAY_USE_TELEGRAM, BTN_YES
from bot.services import SessionState
from bot.states import PhotoStates, RecreatePhotoStates, RegistrationStates
from handlers.photo import (
    go_upload,
    nomination_custom_age,
    nomination_custom_city,
    nomination_custom_gender,
    offer_photo_creation_prompt,
    photo_upload_reject_video,
    photo_upload_reject_video_document,
    photo_uploaded,
    photo_yes,
)
from handlers.registration import (
    complete_registration,
    display_choice_entered,
    display_name_entered,
)
from helpers.aiogram_builders import make_fsm_context, make_message
from helpers.fakes import FakeApiClient


@pytest.mark.asyncio
class TestRegistrationHandlers:
    async def test_display_choice_uses_telegram_username(self) -> None:
        api = FakeApiClient()
        channel_promo = AsyncMock()
        state = await make_fsm_context(data={"username": "cool_user"})
        await state.set_state(RegistrationStates.display_choice)
        message = make_message(BTN_DISPLAY_USE_TELEGRAM)

        with patch("handlers.registration.send_main_menu", new=AsyncMock()):
            await display_choice_entered(message, state, api, channel_promo)

        assert len(api.register_calls) == 1
        assert api.register_calls[0]["use_telegram_username_as_display"] is True
        assert SessionState.REGISTERED in api.session_states
        channel_promo.send_after_registration.assert_awaited_once_with(message.from_user.id)

    async def test_complete_registration_handles_user_already_exists(self) -> None:
        api = FakeApiClient(register_error=ApiError(409, code="UserAlreadyExists"))
        channel_promo = AsyncMock()
        state = await make_fsm_context(data={"username": "dup"})
        message = make_message("ignored")

        with patch("handlers.registration.send_main_menu", new=AsyncMock()) as menu:
            await complete_registration(
                message,
                state,
                api,
                channel_promo,
                use_telegram_username_as_display=True,
            )

        menu.assert_awaited_once()
        assert await state.get_state() is None
        assert SessionState.IDLE in api.session_states
        channel_promo.send_after_registration.assert_not_called()

    async def test_display_name_registration_sends_channel_promo(self) -> None:
        api = FakeApiClient()
        channel_promo = AsyncMock()
        state = await make_fsm_context()
        await state.set_state(RegistrationStates.display_name)
        message = make_message("Custom Name")

        with patch("handlers.registration.send_main_menu", new=AsyncMock()):
            with patch(
                "handlers.registration.offer_photo_after_registration",
                new=AsyncMock(),
            ):
                await display_name_entered(message, state, api, channel_promo)

        assert len(api.register_calls) == 1
        assert api.register_calls[0]["display_name"] == "Custom Name"
        channel_promo.send_after_registration.assert_awaited_once_with(message.from_user.id)

    async def test_registration_continues_when_channel_promo_fails(self) -> None:
        api = FakeApiClient()
        channel_promo = AsyncMock()
        channel_promo.send_after_registration = AsyncMock(return_value=False)
        state = await make_fsm_context(data={"username": "cool_user"})
        message = make_message("ignored")

        with patch("handlers.registration.send_main_menu", new=AsyncMock()):
            with patch(
                "handlers.registration.offer_photo_after_registration",
                new=AsyncMock(),
            ) as offer_photo:
                await complete_registration(
                    message,
                    state,
                    api,
                    channel_promo,
                    use_telegram_username_as_display=True,
                )

        channel_promo.send_after_registration.assert_awaited_once()
        offer_photo.assert_awaited_once()

    async def test_display_name_rejects_invalid_values(self) -> None:
        api = FakeApiClient()
        channel_promo = AsyncMock()
        state = await make_fsm_context()
        await state.set_state(RegistrationStates.display_name)

        await display_name_entered(make_message(""), state, api, channel_promo)
        await display_name_entered(make_message("x" * 33), state, api, channel_promo)

        assert api.register_calls == []
        assert await state.get_state() == RegistrationStates.display_name.state


@pytest.mark.asyncio
class TestPhotoNominationFlow:
    async def test_nomination_accepts_cyrillic_city_from_catalog(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"cities": api.cities, "recreate": False})
        await state.set_state(PhotoStates.custom_city)

        await nomination_custom_city(make_message("Ярославль"), state, api)

        assert (await state.get_data())["nom_city"] == "ярославль"
        assert await state.get_state() == PhotoStates.custom_age.state

    async def test_full_nomination_reaches_upload_state(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"cities": api.cities, "recreate": False})
        await state.set_state(PhotoStates.custom_city)

        await nomination_custom_city(make_message("Moscow"), state, api)
        await nomination_custom_age(make_message("25"), state, api)
        await nomination_custom_gender(make_message("Мужской"), state, api)

        assert await state.get_state() == PhotoStates.upload.state
        nomination = (await state.get_data())["nomination"]
        assert nomination["city"] == "moscow"
        assert nomination["age"] == 25
        assert nomination["gender"] == "Male"

    async def test_invalid_city_stays_on_city_step(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"cities": api.cities})
        await state.set_state(PhotoStates.custom_city)
        message = make_message("Несуществующий Город")

        await nomination_custom_city(message, state, api)

        assert await state.get_state() == PhotoStates.custom_city.state
        message.answer.assert_awaited()

    async def test_invalid_age_does_not_advance(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"cities": api.cities, "nom_city": "moscow"})
        await state.set_state(PhotoStates.custom_age)

        await nomination_custom_age(make_message("13"), state, api)

        assert await state.get_state() == PhotoStates.custom_age.state

    async def test_go_upload_sets_upload_not_blocked_state(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(
            data={
                "recreate": False,
                "nomination": {
                    "useProfileNomination": False,
                    "city": "moscow",
                    "age": 25,
                    "gender": "Male",
                },
            }
        )

        await go_upload(make_message(), state, api)

        assert await state.get_state() == PhotoStates.upload.state

    async def test_photo_yes_starts_nomination_flow(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context()
        await state.set_state(PhotoStates.confirm_create)
        message = make_message(BTN_YES)

        await photo_yes(message, state, api)

        assert await state.get_state() == PhotoStates.custom_city.state
        assert (await state.get_data())["cities"] == api.cities

    async def test_offer_photo_prompt_sets_api_awaiting_photo(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context()
        message = make_message()

        await offer_photo_creation_prompt(message, state, api, 42_001, "Add photo?")

        assert await state.get_state() == PhotoStates.confirm_create.state
        assert SessionState.AWAITING_PHOTO in api.session_states
        message.answer.assert_awaited_once()

    async def test_photo_upload_calls_set_photo_with_nomination(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(
            data={
                "recreate": False,
                "nomination": {
                    "useProfileNomination": False,
                    "city": "moscow",
                    "age": 25,
                    "gender": "Male",
                },
            }
        )
        await state.set_state(PhotoStates.upload)
        status = make_message()
        status.edit_text = AsyncMock()
        message = make_message(photo_file_id="telegram-file-42")

        with patch("handlers.photo._finish_photo_flow", new=AsyncMock()) as finish:
            await photo_uploaded(message, state, api)

        assert len(api.set_photo_calls) == 1
        assert api.set_photo_calls[0]["file_id"] == "telegram-file-42"
        assert api.set_photo_calls[0]["nomination"]["city"] == "moscow"
        finish.assert_awaited_once()
        status.edit_text.assert_not_awaited()

    async def test_photo_upload_without_nomination_does_not_call_api(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"recreate": False})
        await state.set_state(PhotoStates.upload)
        message = make_message(photo_file_id="telegram-file-42")

        with patch("handlers.photo._finish_photo_flow", new=AsyncMock()):
            await photo_uploaded(message, state, api)

        assert api.set_photo_calls == []

    async def test_photo_upload_maps_api_error_to_user_message(self) -> None:
        api = FakeApiClient(set_photo_error=ApiError(400, code="PhotoAlreadyExists"))
        state = await make_fsm_context(
            data={
                "recreate": False,
                "nomination": {
                    "useProfileNomination": False,
                    "city": "moscow",
                    "age": 25,
                    "gender": "Male",
                },
            }
        )
        await state.set_state(PhotoStates.upload)
        status = make_message()
        status.edit_text = AsyncMock()
        message = make_message(photo_file_id="file-1")
        message.answer = AsyncMock(return_value=status)

        with patch("handlers.photo._finish_photo_flow", new=AsyncMock()):
            await photo_uploaded(message, state, api)

        status.edit_text.assert_awaited_once()
        edited = status.edit_text.await_args.args[0]
        assert "фото" in edited.lower()

    async def test_photo_upload_rejects_media_group(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(
            data={
                "recreate": False,
                "nomination": {
                    "useProfileNomination": False,
                    "city": "moscow",
                    "age": 25,
                    "gender": "Male",
                },
            }
        )
        await state.set_state(PhotoStates.upload)
        message = make_message(photo_file_id="file-1", media_group_id="album-1")

        await photo_uploaded(message, state, api)

        assert api.set_photo_calls == []
        message.answer.assert_awaited()
        assert "альбом" in message.answer.await_args.args[0].lower()

    async def test_photo_upload_rejects_video(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(
            data={
                "recreate": False,
                "nomination": {
                    "useProfileNomination": False,
                    "city": "moscow",
                    "age": 25,
                    "gender": "Male",
                },
            }
        )
        await state.set_state(PhotoStates.upload)
        message = make_message(video_file_id="video-1")

        await photo_upload_reject_video(message, state)

        assert api.set_photo_calls == []
        message.answer.assert_awaited_once()
        assert "видео" in message.answer.await_args.args[0].lower()
        assert await state.get_state() == PhotoStates.upload.state

    async def test_photo_upload_rejects_video_note(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"recreate": False, "nomination": {"city": "moscow", "age": 25, "gender": "Male"}})
        await state.set_state(PhotoStates.upload)
        message = make_message(video_note_file_id="note-1")

        await photo_upload_reject_video(message, state)

        assert api.set_photo_calls == []
        assert "видео" in message.answer.await_args.args[0].lower()

    async def test_photo_upload_rejects_video_document(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(data={"recreate": False, "nomination": {"city": "moscow", "age": 25, "gender": "Male"}})
        await state.set_state(PhotoStates.upload)
        message = make_message(document_mime="video/mp4")

        await photo_upload_reject_video_document(message, state)

        assert api.set_photo_calls == []
        assert "видео" in message.answer.await_args.args[0].lower()

    async def test_photo_upload_many_rejects_video(self) -> None:
        api = FakeApiClient()
        state = await make_fsm_context(
            data={
                "recreate": True,
                "replace_all": True,
                "nomination": {"city": "moscow", "age": 25, "gender": "Male"},
                "replace_all_file_ids": [],
            }
        )
        await state.set_state(RecreatePhotoStates.upload_many)
        message = make_message(video_file_id="video-1")

        await photo_upload_reject_video(message, state)

        assert api.set_photo_calls == []
        assert "видео" in message.answer.await_args.args[0].lower()
        assert await state.get_state() == RecreatePhotoStates.upload_many.state
