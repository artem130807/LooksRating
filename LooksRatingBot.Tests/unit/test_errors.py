from __future__ import annotations

import pytest

from bot.errors import ERROR_MESSAGES, translate_error

# API error codes that must stay user-friendly in critical bot flows.
CRITICAL_BOT_ERROR_CODES = [
    "PhotoAlreadyExists",
    "UserProfileIncomplete",
    "RecomendationSettingsIncomplete",
    "InvalidNominationCity",
    "InvalidNominationAge",
    "InvalidNominationGender",
    "UserAlreadyExists",
    "TooManyRequests",
    "PhotoUploadInProgress",
]


@pytest.mark.parametrize("code", CRITICAL_BOT_ERROR_CODES)
def test_critical_error_codes_have_russian_messages(code: str) -> None:
    message = ERROR_MESSAGES.get(code)
    assert message is not None
    assert len(message.strip()) >= 8
    assert translate_error(code) == message
