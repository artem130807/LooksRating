from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any

from api.client import ApiError


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
    ) -> dict[str, Any]:
        self.register_calls.append(
            {
                "telegram_id": telegram_id,
                "telegram_username": telegram_username,
                "use_telegram_username_as_display": use_telegram_username_as_display,
                "display_name": display_name,
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

    async def upsert_recommendation_settings(
        self,
        telegram_id: int,
        age: int,
        gender: int,
        city: str,
    ) -> None:
        return None
