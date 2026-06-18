from __future__ import annotations

from bot import texts
from services.referral_program import ReferralLinkView


def format_referral_program_message(view: ReferralLinkView) -> str:
    link_body = (
        texts.REFERRAL_PROGRAM_LINK_NEW.format(link=view.link)
        if view.was_created
        else texts.REFERRAL_PROGRAM_LINK_EXISTING.format(link=view.link)
    )
    stats = texts.REFERRAL_PROGRAM_INVITE_STATS.format(
        invited=view.invited_count,
        max_invited=view.max_invited,
    )
    return f"{texts.REFERRAL_PROGRAM_INTRO}\n\n{stats}\n\n{link_body}"
