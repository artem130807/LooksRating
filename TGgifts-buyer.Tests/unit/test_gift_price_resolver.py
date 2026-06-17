from __future__ import annotations

import pytest

from helpers.fakes import FakePyrogramClient
from services.gift_price_resolver import resolve_gift_ids_by_price


@pytest.mark.asyncio
class TestResolveGiftIdsByPrice:
    async def test_resolves_requested_prices(self) -> None:
        app = FakePyrogramClient()

        resolved = await resolve_gift_ids_by_price(app, {400, 150})

        assert resolved == {400: 101, 150: 105}

    async def test_skips_gifts_without_id_or_price(self) -> None:
        app = FakePyrogramClient(
            gifts=[
                {"id": None, "price": 400},
                {"price": 300},
                {"id": 201, "price": 300},
            ]
        )

        resolved = await resolve_gift_ids_by_price(app, {300, 400})

        assert resolved == {300: 201}

    async def test_uses_first_gift_per_price(self) -> None:
        app = FakePyrogramClient(
            gifts=[
                {"id": 301, "price": 200},
                {"id": 302, "price": 200},
            ]
        )

        resolved = await resolve_gift_ids_by_price(app, {200})

        assert resolved == {200: 301}

    async def test_returns_empty_for_unknown_prices(self) -> None:
        app = FakePyrogramClient(gifts=[{"id": 1, "price": 50}])

        resolved = await resolve_gift_ids_by_price(app, {999})

        assert resolved == {}
