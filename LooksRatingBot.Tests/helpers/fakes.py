from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from api.client import ApiError
from api.dto import UserReferenceLinkData


@dataclass
class FakeApiClient:
    """In-memory API stub for handler tests."""

    cities: list[str] = field(default_factory=lambda: ["moscow", "saint-petersburg", "ярославль"])
    user: dict[str, Any] | None = None
    session: dict[str, Any] | None = None
    my_photo: dict[str, Any] | None = None
    register_result: dict[str, Any] = field(
        default_factory=lambda: {"displayName": "Test User", "userId": "user-1"}
    )
    set_photo_result: dict[str, Any] = field(
        default_factory=lambda: {"city": "moscow", "rating": 7.5, "ratingCount": 0}
    )
    register_error: ApiError | None = None
    set_photo_error: ApiError | None = None
    cities_error: ApiError | None = None
    session_error: ApiError | None = None

    session_states: list[str] = field(default_factory=list)
    set_photo_calls: list[dict[str, Any]] = field(default_factory=list)
    register_calls: list[dict[str, Any]] = field(default_factory=list)
    referral_link: str | None = None
    referral_invited_count: int = 0
    referral_max_invited: int = 5
    referral_get_error: ApiError | None = None
    referral_create_error: ApiError | None = None
    get_referral_link_calls: list[int] = field(default_factory=list)
    create_referral_link_calls: list[int] = field(default_factory=list)
    payment_orders: list[dict[str, Any]] = field(default_factory=list)

    async def get_cities(self) -> list[str]:
        if self.cities_error:
            raise self.cities_error
        return list(self.cities)

    async def get_user(self, telegram_id: int) -> dict[str, Any] | None:
        return self.user

    async def get_session(self, telegram_id: int) -> dict[str, Any] | None:
        if self.session_error:
            raise self.session_error
        return self.session

    async def ensure_session(self, telegram_id: int, initial_state: str | None = None) -> dict[str, Any]:
        if self.session is None:
            self.session = {"telegramId": telegram_id, "state": initial_state or "Start"}
        return self.session

    async def update_session_state(self, telegram_id: int, state: str) -> dict[str, Any]:
        self.session_states.append(state)
        if self.session is None:
            self.session = {"telegramId": telegram_id}
        self.session["state"] = state
        return self.session

    async def register_user(
        self,
        telegram_id: int,
        telegram_username: str | None,
        *,
        use_telegram_username_as_display: bool,
        display_name: str | None = None,
        referral_link: str | None = None,
    ) -> dict[str, Any]:
        self.register_calls.append(
            {
                "telegram_id": telegram_id,
                "telegram_username": telegram_username,
                "use_telegram_username_as_display": use_telegram_username_as_display,
                "display_name": display_name,
                "referral_link": referral_link,
            }
        )
        if self.register_error:
            raise self.register_error
        return dict(self.register_result)

    async def get_my_photo(self, telegram_id: int) -> dict[str, Any] | None:
        return self.my_photo

    async def set_photo(
        self,
        telegram_id: int,
        file_id: str,
        nomination: dict[str, Any],
    ) -> dict[str, Any]:
        self.set_photo_calls.append(
            {
                "telegram_id": telegram_id,
                "file_id": file_id,
                "nomination": nomination,
            }
        )
        if self.set_photo_error:
            raise self.set_photo_error
        return dict(self.set_photo_result)

    async def recreate_photo(
        self,
        telegram_id: int,
        file_id: str,
        nomination: dict[str, Any],
        *,
        target_photo_id: str | None = None,
    ) -> dict[str, Any]:
        self.set_photo_calls.append(
            {
                "telegram_id": telegram_id,
                "file_id": file_id,
                "nomination": nomination,
                "target_photo_id": target_photo_id,
                "recreate": True,
            }
        )
        if self.set_photo_error:
            raise self.set_photo_error
        return dict(self.set_photo_result)

    async def get_user_reference_link(self, telegram_id: int) -> UserReferenceLinkData | None:
        self.get_referral_link_calls.append(telegram_id)
        if self.referral_get_error:
            raise self.referral_get_error
        if self.referral_link is None:
            return None
        return UserReferenceLinkData(
            link=self.referral_link,
            count_invited=self.referral_invited_count,
            max_invited=self.referral_max_invited,
        )

    async def create_user_reference_link(self, telegram_id: int) -> UserReferenceLinkData:
        self.create_referral_link_calls.append(telegram_id)
        if self.referral_create_error:
            raise self.referral_create_error
        if self.referral_link is None:
            self.referral_link = f"https://t.me/LooksRatingBot?start={telegram_id:012x}"
        return UserReferenceLinkData(
            link=self.referral_link,
            count_invited=self.referral_invited_count,
            max_invited=self.referral_max_invited,
        )

    async def create_payment_order(self, telegram_id: int, product_code: int) -> dict[str, Any]:
        order = {
            "payload": f"vip-{telegram_id}",
            "amountStars": 140,
            "currency": "XTR",
            "productCode": product_code,
            "productName": "VIP",
        }
        self.payment_orders.append(order)
        return order

    async def confirm_payment_order(
        self,
        telegram_id: int,
        payload: str,
        telegram_payment_charge_id: str,
        provider_payment_charge_id: str | None = None,
    ) -> dict[str, Any]:
        return {"ok": True}

    async def upsert_recommendation_settings(
        self,
        telegram_id: int,
        age: int,
        gender: int,
        city: str,
    ) -> None:
        return None
