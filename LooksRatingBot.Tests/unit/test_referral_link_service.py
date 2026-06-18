import pytest

from api.client import ApiError
from api.dto import REFERRAL_MAX_INVITED_DEFAULT, UserReferenceLinkData
from bot.referral_presenter import format_referral_program_message
from bot import texts
from helpers.fakes import FakeApiClient
from services.referral_program import ReferralLinkService, ReferralLinkView


@pytest.mark.asyncio
async def test_get_or_create_link_returns_existing_without_create() -> None:
    api = FakeApiClient(
        referral_link="https://t.me/LooksRatingBot?start=existing-guid",
        referral_invited_count=2,
        referral_max_invited=5,
    )
    service = ReferralLinkService(api)

    view = await service.get_or_create_link(42_001)

    assert view.link.endswith("existing-guid")
    assert view.was_created is False
    assert view.invited_count == 2
    assert view.max_invited == 5
    assert api.create_referral_link_calls == []


@pytest.mark.asyncio
async def test_get_or_create_link_creates_when_missing() -> None:
    api = FakeApiClient(referral_link=None)
    service = ReferralLinkService(api)

    view = await service.get_or_create_link(42_002)

    assert view.was_created is True
    assert view.invited_count == 0
    assert view.max_invited == REFERRAL_MAX_INVITED_DEFAULT
    assert api.create_referral_link_calls == [42_002]


@pytest.mark.asyncio
async def test_get_or_create_link_propagates_create_errors() -> None:
    api = FakeApiClient(
        referral_link=None,
        referral_create_error=ApiError(500, message="server error"),
    )
    service = ReferralLinkService(api)

    with pytest.raises(ApiError):
        await service.get_or_create_link(42_003)


def test_user_reference_link_data_from_payload_defaults() -> None:
    data = UserReferenceLinkData.from_payload({"link": "https://t.me/bot?start=1"})

    assert data.count_invited == 0
    assert data.max_invited == REFERRAL_MAX_INVITED_DEFAULT


def test_user_reference_link_data_from_payload_rejects_missing_link() -> None:
    import pytest

    with pytest.raises(ValueError, match="missing"):
        UserReferenceLinkData.from_payload({})


def test_format_referral_program_message_shows_invite_counter() -> None:
    view = ReferralLinkView(
        link="https://t.me/LooksRatingBot?start=abc",
        was_created=False,
        invited_count=3,
        max_invited=5,
    )

    message = format_referral_program_message(view)

    assert "3/5" in message
    assert "приглашённых пользователей" in message
    assert texts.REFERRAL_PROGRAM_INTRO in message
    assert "abc" in message


def test_format_referral_program_message_shows_zero_invites() -> None:
    view = ReferralLinkView(
        link="https://t.me/LooksRatingBot?start=abc",
        was_created=True,
        invited_count=0,
        max_invited=5,
    )

    message = format_referral_program_message(view)

    assert "0/5" in message
    assert texts.REFERRAL_PROGRAM_LINK_NEW.split("{")[0].strip() in message
