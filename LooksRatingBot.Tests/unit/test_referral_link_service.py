import pytest

from api.client import ApiError
from helpers.fakes import FakeApiClient
from services.referral_program import ReferralLinkService


@pytest.mark.asyncio
async def test_get_or_create_link_returns_existing_without_create() -> None:
    api = FakeApiClient(
        referral_link="https://t.me/LooksRatingBot?start=existing-guid",
    )
    service = ReferralLinkService(api)

    view = await service.get_or_create_link(42_001)

    assert view.link.endswith("existing-guid")
    assert view.was_created is False
    assert api.get_referral_link_calls == [42_001]
    assert api.create_referral_link_calls == []


@pytest.mark.asyncio
async def test_get_or_create_link_creates_when_missing() -> None:
    api = FakeApiClient(referral_link=None)
    service = ReferralLinkService(api)

    view = await service.get_or_create_link(42_002)

    assert view.link.startswith("https://t.me/LooksRatingBot?start=")
    assert view.was_created is True
    assert api.get_referral_link_calls == [42_002]
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
