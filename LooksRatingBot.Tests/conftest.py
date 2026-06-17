from __future__ import annotations

import pytest

from helpers.aiogram_builders import make_fsm_context, make_message
from helpers.fakes import FakeApiClient

TELEGRAM_ID = 42_001


@pytest.fixture
def telegram_id() -> int:
    return TELEGRAM_ID


@pytest.fixture
def api() -> FakeApiClient:
    return FakeApiClient()


@pytest.fixture
async def fsm():
    return await make_fsm_context(user_id=TELEGRAM_ID)


@pytest.fixture
def message(telegram_id: int):
    return make_message(user_id=telegram_id)
