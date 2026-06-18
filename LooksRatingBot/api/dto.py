from __future__ import annotations

from dataclasses import dataclass
from typing import Any

REFERRAL_MAX_INVITED_DEFAULT = 5


@dataclass(frozen=True, slots=True)
class UserReferenceLinkData:
    link: str
    count_invited: int
    max_invited: int

    @classmethod
    def from_payload(cls, payload: dict[str, Any]) -> UserReferenceLinkData:
        link = payload.get("link")
        if not link:
            raise ValueError("Referral link missing in API response")

        count_invited = int(payload.get("countInvited", 0))
        max_invited = int(payload.get("maxInvited", REFERRAL_MAX_INVITED_DEFAULT))
        if count_invited < 0:
            count_invited = 0
        if max_invited <= 0:
            max_invited = REFERRAL_MAX_INVITED_DEFAULT

        return cls(
            link=str(link),
            count_invited=count_invited,
            max_invited=max_invited,
        )
