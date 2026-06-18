from __future__ import annotations

from dataclasses import dataclass

from api.client import LooksRatingApiClient
from api.dto import UserReferenceLinkData


@dataclass(frozen=True, slots=True)
class ReferralLinkView:
    link: str
    was_created: bool
    invited_count: int
    max_invited: int


class ReferralLinkService:
    """Fetches an existing referral link or creates one idempotently via the API."""

    def __init__(self, api: LooksRatingApiClient) -> None:
        self._api = api

    async def get_or_create_link(self, telegram_id: int) -> ReferralLinkView:
        existing = await self._api.get_user_reference_link(telegram_id)
        if existing is not None:
            return self._to_view(existing, was_created=False)

        created = await self._api.create_user_reference_link(telegram_id)
        return self._to_view(created, was_created=True)

    @staticmethod
    def _to_view(data: UserReferenceLinkData, *, was_created: bool) -> ReferralLinkView:
        return ReferralLinkView(
            link=data.link,
            was_created=was_created,
            invited_count=data.count_invited,
            max_invited=data.max_invited,
        )
