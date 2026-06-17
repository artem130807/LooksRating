from __future__ import annotations

from typing import Any

import aiohttp


class ApiError(Exception):
    def __init__(self, status: int, message: str | None = None):
        self.status = status
        self.message = message or f"HTTP {status}"
        super().__init__(self.message)


class TicketApiClient:
    def __init__(self, base_url: str, api_key: str = "", timeout_seconds: float = 30.0):
        self._base_url = base_url.rstrip("/")
        self._api_key = api_key
        self._timeout = aiohttp.ClientTimeout(total=timeout_seconds)
        self._session: aiohttp.ClientSession | None = None

    async def start(self) -> None:
        headers = {"Content-Type": "application/json"}
        if self._api_key:
            headers["X-Api-Key"] = self._api_key
        self._session = aiohttp.ClientSession(headers=headers, timeout=self._timeout)

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
    ) -> Any:
        if not self._session:
            raise RuntimeError("API client is not started")

        url = f"{self._base_url}{path}"
        async with self._session.request(method, url, json=json, params=params) as resp:
            body: Any = None
            if resp.content_length != 0 or resp.status != 204:
                try:
                    body = await resp.json()
                except aiohttp.ContentTypeError:
                    body = None

            if resp.status >= 400:
                message = None
                if isinstance(body, dict):
                    message = body.get("error")
                raise ApiError(resp.status, message=message)

            return body

    async def check_connection(self) -> None:
        await self._request("GET", "/health")

    async def get_session(self, telegram_id: int) -> dict[str, Any] | None:
        try:
            return await self._request(
                "GET",
                "/api/sessions",
                params={"telegramId": telegram_id},
            )
        except ApiError as exc:
            if exc.status == 404:
                return None
            raise

    async def ensure_session(self, telegram_id: int) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/sessions/ensure",
            json={"telegramId": telegram_id},
        )

    async def begin_login(self, telegram_id: int) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/sessions/begin-login",
            json={"telegramId": telegram_id},
        )

    async def submit_login(self, telegram_id: int, login: str) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/sessions/submit-login",
            json={"telegramId": telegram_id, "login": login},
        )

    async def authenticate(self, telegram_id: int, login: str, password: str) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/sessions/authenticate",
            json={"telegramId": telegram_id, "login": login, "password": password},
        )

    async def logout(self, telegram_id: int) -> None:
        await self._request(
            "POST",
            "/api/sessions/logout",
            json={"telegramId": telegram_id},
        )

    async def list_cities(self, telegram_id: int) -> list[str]:
        data = await self._request(
            "GET",
            "/api/moderation/cities",
            params={"telegramId": telegram_id},
        )
        return list(data.get("cities", []))

    async def select_city(self, telegram_id: int, city: str) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/moderation/select-city",
            json={"telegramId": telegram_id, "city": city},
        )

    async def get_current_ticket(self, telegram_id: int) -> dict[str, Any]:
        return await self._request(
            "GET",
            "/api/moderation/current",
            params={"telegramId": telegram_id},
        )

    async def skip_current(self, telegram_id: int) -> None:
        await self._request(
            "POST",
            "/api/moderation/skip",
            json={"telegramId": telegram_id},
        )

    async def dismiss_current(self, telegram_id: int) -> None:
        await self._request(
            "POST",
            "/api/moderation/dismiss",
            json={"telegramId": telegram_id},
        )

    async def delete_current(self, telegram_id: int) -> None:
        await self._request(
            "POST",
            "/api/moderation/delete",
            json={"telegramId": telegram_id},
        )

    async def delete_current_account(self, telegram_id: int) -> None:
        await self._request(
            "POST",
            "/api/moderation/delete-account",
            json={"telegramId": telegram_id},
        )

    async def monitoring_status(self, telegram_id: int) -> dict[str, Any]:
        return await self._request(
            "GET",
            "/api/monitoring/status",
            params={"telegramId": telegram_id},
        )

    async def monitoring_run(self, telegram_id: int) -> dict[str, Any]:
        return await self._request(
            "POST",
            "/api/monitoring/run",
            json={"telegramId": telegram_id},
        )

    async def monitoring_alerts(self, telegram_id: int) -> list[dict[str, Any]]:
        data = await self._request(
            "GET",
            "/api/monitoring/alerts",
            params={"telegramId": telegram_id},
        )
        return list(data.get("alerts") or [])

    async def monitoring_logs(self, telegram_id: int, service: str, tail: int = 80) -> dict[str, Any]:
        return await self._request(
            "GET",
            "/api/monitoring/logs",
            params={"telegramId": telegram_id, "service": service, "tail": tail},
        )

    async def monitoring_pending_alerts(self) -> dict[str, Any]:
        return await self._request("GET", "/api/monitoring/alerts/pending")

    async def monitoring_ack_alert(self, alert_id: int) -> None:
        await self._request("POST", f"/api/monitoring/alerts/{alert_id}/ack")
