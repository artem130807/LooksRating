from __future__ import annotations

import uuid
from typing import Any

import aiohttp

from api.dto import UserReferenceLinkData
from bot.gender_api import gender_to_api


class ApiError(Exception):
    def __init__(self, status: int, code: str | None = None, message: str | None = None):
        self.status = status
        self.code = code
        self.message = message or code or f"HTTP {status}"
        super().__init__(self.message)


class LooksRatingApiClient:
    def __init__(self, base_url: str, api_key: str = ""):
        self._base_url = base_url.rstrip("/")
        self._api_key = api_key
        self._session: aiohttp.ClientSession | None = None

    async def start(self) -> None:
        headers = {"Content-Type": "application/json"}
        if self._api_key:
            headers["X-Api-Key"] = self._api_key
        timeout = aiohttp.ClientTimeout(total=30, connect=5, sock_connect=5)
        self._session = aiohttp.ClientSession(headers=headers, timeout=timeout)

    async def close(self) -> None:
        if self._session:
            await self._session.close()
            self._session = None

    async def _request(
        self,
        method: str,
        path: str,
        *,
        json: Any = None,
        params: dict[str, Any] | None = None,
        allow_404: bool = False,
    ) -> Any:
        if not self._session:
            raise RuntimeError("API client is not started")
        url = f"{self._base_url}{path}"
        async with self._session.request(method, url, json=json, params=params) as resp:
            if resp.status == 404 and allow_404:
                return None
            body: Any = None
            if resp.content_length != 0 or resp.status != 204:
                try:
                    body = await resp.json()
                except aiohttp.ContentTypeError:
                    body = None
            if resp.status >= 400:
                code = None
                message = None
                if isinstance(body, dict):
                    code = body.get("error")
                    message = code or body.get("title") or body.get("detail")
                raise ApiError(resp.status, code=code, message=message)
            return body

    async def check_connection(self) -> None:
        await self._request("GET", "/health/ready")

    async def get_cities(self) -> list[str]:
        data = await self._request("GET", "/api/cities")
        return list(data.get("cities", []))

    async def register_user(
        self,
        telegram_id: int,
        telegram_username: str | None,
        *,
        use_telegram_username_as_display: bool,
        display_name: str | None = None,
        referral_link: str | None = None,
    ) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/users/register",
            json={
                "telegramId": telegram_id,
                "telegramUsername": telegram_username,
                "useTelegramUsernameAsDisplay": use_telegram_username_as_display,
                "name": display_name,
                "link": referral_link,
            },
        )

    async def upsert_recommendation_settings(
        self,
        telegram_id: int,
        age: int,
        gender: int,
        city: str,
    ) -> None:
        await self._request(
            "PUT",
            "/api/recomendation-settings",
            json={
                "telegramId": telegram_id,
                "age": age,
                "gender": gender_to_api(gender),
                "city": city,
            },
        )

    async def get_recommendation_settings(self, telegram_id: int) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            f"/api/recomendation-settings/{telegram_id}",
            allow_404=True,
        )

    async def get_user(self, telegram_id: int) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            f"/api/users/{telegram_id}",
            allow_404=True,
        )

    async def update_gender(self, telegram_id: int, gender: int) -> None:
        await self._request(
            "PUT",
            "/api/users/gender",
            json={"telegramId": telegram_id, "gender": gender_to_api(gender)},
        )

    async def update_city(self, telegram_id: int, city: str) -> None:
        await self._request(
            "PUT",
            "/api/users/city",
            json={"telegramId": telegram_id, "city": city},
        )

    async def update_age(self, telegram_id: int, age: int) -> None:
        await self._request(
            "PUT",
            "/api/users/age",
            json={"telegramId": telegram_id, "age": age},
        )

    async def ensure_session(
        self,
        telegram_id: int,
        initial_state: str | None = None,
    ) -> dict[str, Any]:
        payload: dict[str, Any] = {"telegramId": telegram_id}
        if initial_state:
            payload["initialState"] = initial_state
        return await self._request("POST", "/api/user-sessions/ensure", json=payload)

    async def get_session(self, telegram_id: int) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            f"/api/user-sessions/{telegram_id}",
            allow_404=True,
        )

    async def update_session_state(self, telegram_id: int, state: str) -> dict[str, Any]:
        return await self._request(
            "PUT",
            "/api/user-sessions/state",
            json={"telegramId": telegram_id, "state": state},
        )

    async def link_session(self, telegram_id: int, user_id: str) -> dict[str, Any]:
        return await self._request(
            "PUT",
            "/api/user-sessions/link",
            json={"telegramId": telegram_id, "userId": user_id},
        )

    async def set_photo(
        self,
        telegram_id: int,
        file_id: str,
        nomination: dict[str, Any],
    ) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/photo-users/set_photo",
            json={
                "telegramId": telegram_id,
                "telegramFileId": file_id,
                "nomination": nomination,
            },
        )

    async def recreate_photo(
        self,
        telegram_id: int,
        file_id: str,
        nomination: dict[str, Any],
        *,
        target_photo_id: str | None = None,
    ) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "telegramId": telegram_id,
            "telegramFileId": file_id,
            "nomination": nomination,
        }
        if target_photo_id:
            payload["targetPhotoId"] = str(uuid.UUID(str(target_photo_id)))
        return await self._request(
            "POST",
            "/api/photo-users/recreate_photo",
            json=payload,
        )

    async def recreate_all_photos(
        self,
        telegram_id: int,
        file_ids: list[str],
        nomination: dict[str, Any],
    ) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/photo-users/recreate_all_photos",
            json={
                "telegramId": telegram_id,
                "telegramFileIds": file_ids,
                "nomination": nomination,
            },
        )

    async def get_my_photo(self, telegram_id: int) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            f"/api/photo-users/my/{telegram_id}",
            allow_404=True,
        )

    async def get_photo_user_by_id(self, profile_id: str) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            f"/api/photo-users/{profile_id}",
            allow_404=True,
        )

    async def get_next_photo(self, telegram_id: int) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/photo-users/get_next_photo",
            json={"telegramId": telegram_id},
        )

    async def get_top_photos(
        self,
        telegram_id: int,
        gender: int,
        age: int,
        *,
        season_id: str | None = None,
        page: int = 1,
        page_size: int = 10,
    ) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "telegramId": telegram_id,
            "genderEnum": gender_to_api(gender),
            "age": age,
            "page": page,
            "pageSize": page_size,
        }
        if season_id:
            payload["seasonId"] = season_id
        return await self._request("POST", "/api/photo-users/get_top_photos", json=payload)

    async def get_the_best_week_photos_ids(self) -> list[int]:
        data = await self._request("GET", "/api/photo-users/get_theBestWeek_photosId")
        if isinstance(data, list):
            return [int(item) for item in data]
        return []

    async def get_the_best_week_photos_now(
        self,
        telegram_id: int,
        gender: int,
        age: int,
    ) -> list[dict[str, Any]]:
        data = await self._request(
            "GET",
            "/api/photo-users/get_thebestWeek_photosNow",
            params={
                "telegramId": telegram_id,
                "genderEnum": gender_to_api(gender),
                "age": age,
            },
            allow_404=True,
        )
        if isinstance(data, list):
            return data
        return []

    async def get_the_best_vip_photos(
        self,
        telegram_id: int,
        gender: int,
        age: int,
    ) -> list[dict[str, Any]]:
        data = await self._request(
            "GET",
            "/api/photo-users/get_thebestvip_photos",
            params={
                "telegramId": telegram_id,
                "genderEnum": gender_to_api(gender),
                "age": age,
            },
            allow_404=True,
        )
        if isinstance(data, list):
            return data
        return []

    async def get_the_best_week_photos(
        self,
        telegram_id: int,
        gender: int,
        age: int,
    ) -> list[dict[str, Any]]:
        data = await self._request(
            "GET",
            "/api/photo-users/get_thebestWeek_photos",
            params={
                "telegramId": telegram_id,
                "genderEnum": gender_to_api(gender),
                "age": age,
            },
            allow_404=True,
        )
        if isinstance(data, list):
            return data
        return []

    async def get_user_stats(self, telegram_id: int) -> dict[str, Any]:
        return await self._request("GET", f"/api/users/{telegram_id}/stats")

    async def delete_account(self, telegram_id: int) -> None:
        await self._request("DELETE", f"/api/users/{telegram_id}")

    async def get_my_photo_by_season(
        self, telegram_id: int, season_id: str
    ) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            f"/api/photo-users/my/{telegram_id}/seasons/{season_id}",
            allow_404=True,
        )

    async def create_review(
        self,
        reviewer_telegram_id: int,
        *,
        rating: int,
        photo_profile_id: str | None = None,
    ) -> dict[str, Any]:
        payload: dict[str, Any] = {
            "reviewerTelegramId": reviewer_telegram_id,
            "rating": rating,
        }
        if photo_profile_id:
            payload["photoProfileId"] = str(uuid.UUID(str(photo_profile_id)))
        return await self._request(
            "POST",
            "/api/reviews/create_review",
            json=payload,
        )

    async def create_ticket(
        self,
        reporter_telegram_id: int,
        description: str,
        *,
        photo_profile_id: str | None = None,
    ) -> dict[str, Any]:
        text = description.strip()
        payload: dict[str, Any] = {
            "reporterTelegramId": reporter_telegram_id,
            "description": text,
        }
        if photo_profile_id:
            payload["photoProfileId"] = str(uuid.UUID(str(photo_profile_id)))
        return await self._request(
            "POST",
            "/api/user-tickets/create",
            json=payload,
        )

    async def get_current_season(self) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            "/api/seasons/current",
            allow_404=True,
        )

    async def get_latest_chapter(self) -> dict[str, Any] | None:
        return await self._request(
            "GET",
            "/api/list-seasons/latest",
            params={"includeSeasons": "true"},
            allow_404=True,
        )

    async def get_chapters(self, *, include_seasons: bool = True) -> list[dict[str, Any]]:
        data = await self._request(
            "GET",
            "/api/list-seasons",
            params={"includeSeasons": str(include_seasons).lower()},
        )
        if isinstance(data, list):
            return data
        return list(data.get("items", [])) if isinstance(data, dict) else []

    async def get_seasons_by_chapter(self, chapter_id: str) -> list[dict[str, Any]]:
        data = await self._request(
            "GET",
            f"/api/seasons/by-chapter/{chapter_id}",
            params={"includeClosed": "true"},
            allow_404=True,
        )
        if not data:
            return []
        if isinstance(data, list):
            return data
        return list(data.get("seasons", []))

    async def create_payment_order(self, telegram_id: int, product_code: int) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/payments/orders",
            json={
                "telegramId": telegram_id,
                "productCode": product_code,
            },
        )

    async def confirm_payment_order(
        self,
        telegram_id: int,
        payload: str,
        telegram_payment_charge_id: str,
        provider_payment_charge_id: str | None = None,
    ) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/payments/orders/confirm",
            json={
                "telegramId": telegram_id,
                "payload": payload,
                "telegramPaymentChargeId": telegram_payment_charge_id,
                "providerPaymentChargeId": provider_payment_charge_id,
            },
        )

    async def get_user_reference_link(self, telegram_id: int) -> UserReferenceLinkData | None:
        data = await self._request(
            "GET",
            f"/api/user-reference-links/{telegram_id}",
            allow_404=True,
        )
        if data is None:
            return None
        return UserReferenceLinkData.from_payload(data)

    async def create_user_reference_link(self, telegram_id: int) -> UserReferenceLinkData:
        data = await self._request(
            "POST",
            f"/api/user-reference-links/{telegram_id}",
        )
        if not data or not data.get("link"):
            raise ApiError(502, message="Referral link missing in API response")
        return UserReferenceLinkData.from_payload(data)

    async def get_pending_review_milestone_notifications(self) -> list[dict[str, Any]]:
        data = await self._request("GET", "/api/reviews/milestone-notifications/pending")
        if isinstance(data, list):
            return data
        return []

    async def ack_review_milestone_notification(self, notification_id: str) -> None:
        await self._request(
            "POST",
            f"/api/reviews/milestone-notifications/{notification_id}/ack",
        )

    async def get_review_milestone_reviewers(self, notification_id: str) -> dict[str, Any]:
        data = await self._request(
            "GET",
            f"/api/reviews/milestone-notifications/{notification_id}/reviewers",
        )
        return data if isinstance(data, dict) else {}
