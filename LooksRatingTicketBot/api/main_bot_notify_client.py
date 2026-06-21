from __future__ import annotations

import asyncio
from dataclasses import dataclass

import aiohttp


@dataclass(frozen=True)
class MainBotNotifyResult:
    success: bool
    message: str


class MainBotNotifyClient:
    _RETRY_DELAYS_SECONDS = (0.0, 0.3, 1.0)

    def __init__(
        self,
        base_url: str,
        api_key: str,
        *,
        timeout_seconds: float = 10.0,
    ):
        self._base_url = base_url.rstrip("/")
        self._api_key = api_key
        self._timeout = aiohttp.ClientTimeout(total=timeout_seconds)

    async def notify_writing_off_sparks_confirmed(
        self,
        *,
        telegram_id: int,
        stars: int,
    ) -> MainBotNotifyResult:
        if not self._base_url:
            return MainBotNotifyResult(success=False, message="Main bot notify URL is not set")

        url = f"{self._base_url}/internal/notifications/writing-off-sparks-confirmed"
        headers = {
            "Content-Type": "application/json",
            "X-Internal-Notify-Key": self._api_key,
        }
        payload = {
            "telegram_id": telegram_id,
            "stars": stars,
        }
        return await self._post_with_retries(url, headers, payload)

    async def notify_writing_off_sparks_cancelled(
        self,
        *,
        telegram_id: int,
        stars: int,
        sparks_count: int,
    ) -> MainBotNotifyResult:
        if not self._base_url:
            return MainBotNotifyResult(success=False, message="Main bot notify URL is not set")

        url = f"{self._base_url}/internal/notifications/writing-off-sparks-cancelled"
        headers = {
            "Content-Type": "application/json",
            "X-Internal-Notify-Key": self._api_key,
        }
        payload = {
            "telegram_id": telegram_id,
            "stars": stars,
            "sparks_count": sparks_count,
        }
        return await self._post_with_retries(url, headers, payload)

    async def _post_with_retries(
        self,
        url: str,
        headers: dict[str, str],
        payload: dict[str, int],
    ) -> MainBotNotifyResult:
        last_message = "Unknown error"

        for delay in self._RETRY_DELAYS_SECONDS:
            if delay:
                await asyncio.sleep(delay)
            try:
                async with aiohttp.ClientSession(timeout=self._timeout) as session:
                    async with session.post(url, json=payload, headers=headers) as response:
                        data = await response.json(content_type=None)
                        if response.status == 200 and bool(data.get("success")):
                            return MainBotNotifyResult(
                                success=True,
                                message=str(data.get("message") or ""),
                            )
                        last_message = str(data.get("message") or f"HTTP {response.status}")
                        if response.status < 500:
                            return MainBotNotifyResult(success=False, message=last_message)
            except (aiohttp.ClientError, TimeoutError) as exc:
                last_message = str(exc)

        return MainBotNotifyResult(success=False, message=last_message)
