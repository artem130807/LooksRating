from __future__ import annotations

from dataclasses import dataclass

from api.client import ApiError, LooksRatingApiClient


@dataclass(frozen=True, slots=True)
class ReferralLinkView:
    link: str
    was_created: bool


class ReferralLinkService:
    """Fetches an existing referral link or creates one idempotently via the API."""

    def __init__(self, api: LooksRatingApiClient) -> None:
        self._api = api

    async def get_or_create_link(self, telegram_id: int) -> ReferralLinkView:
        existing = await self._api.get_user_reference_link(telegram_id)
        if existing and existing.get("link"):
            return ReferralLinkView(link=str(existing["link"]), was_created=False)

        try:
            created = await self._api.create_user_reference_link(telegram_id)
        except ApiError:
            raise

        link = created.get("link") if created else None
        if not link:
            raise ApiError(502, message="Referral link missing in API response")

        return ReferralLinkView(link=str(link), was_created=True)
