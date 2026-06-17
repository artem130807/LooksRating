from __future__ import annotations

import logging
from typing import Any

import aiohttp

from bot.http_session import create_client_session

logger = logging.getLogger(__name__)


class MainBotMediaError(Exception):
    """Failed to download a Telegram file using the main rating bot token."""


class MainBotMediaService:
    """Downloads user photos via the main LooksRating bot token.

    Telegram file_id values are bot-specific. Moderation runs in TicketBot,
    but profile photos were uploaded through the main bot — we re-fetch bytes
    with the main token and re-upload them to admins from TicketBot.
    """

    def __init__(
        self,
        bot_token: str,
        *,
        proxy: str | None = None,
        timeout_seconds: float = 45.0,
    ):
        token = bot_token.strip()
        if not token:
            raise ValueError("Main bot token is required for moderation photos")
        self._bot_token = token
        self._proxy = proxy
        self._timeout_seconds = timeout_seconds
        self._timeout = aiohttp.ClientTimeout(total=timeout_seconds)
        self._session: aiohttp.ClientSession | None = None

    async def start(self) -> None:
        if self._session is None:
            self._session = create_client_session(
                proxy=self._proxy,
                timeout_seconds=self._timeout_seconds,
            )

    async def close(self) -> None:
        if self._session is not None:
            await self._session.close()
            self._session = None

    async def download_photo_bytes(self, file_id: str) -> bytes:
        if not file_id.strip():
            raise MainBotMediaError("empty file_id")

        session = self._require_session()
        file_path = await self._resolve_file_path(session, file_id)
        return await self._download_file(session, file_path)

    async def _resolve_file_path(self, session: aiohttp.ClientSession, file_id: str) -> str:
        url = f"https://api.telegram.org/bot{self._bot_token}/getFile"
        try:
            async with session.get(url, params={"file_id": file_id}) as response:
                payload = await self._read_json(response)
        except (aiohttp.ClientError, TimeoutError) as exc:
            raise MainBotMediaError(f"getFile network error: {exc}") from exc

        if not payload.get("ok"):
            description = str(payload.get("description") or "getFile failed")
            raise MainBotMediaError(description)

        result = payload.get("result") or {}
        file_path = result.get("file_path")
        if not file_path:
            raise MainBotMediaError("getFile returned empty file_path")
        return str(file_path)

    async def _download_file(self, session: aiohttp.ClientSession, file_path: str) -> bytes:
        url = f"https://api.telegram.org/file/bot{self._bot_token}/{file_path}"
        try:
            async with session.get(url) as response:
                if response.status >= 400:
                    raise MainBotMediaError(f"file download HTTP {response.status}")
                data = await response.read()
        except (aiohttp.ClientError, TimeoutError) as exc:
            raise MainBotMediaError(f"download network error: {exc}") from exc

        if not data:
            raise MainBotMediaError("downloaded file is empty")
        return data

    @staticmethod
    async def _read_json(response: aiohttp.ClientResponse) -> dict[str, Any]:
        try:
            payload = await response.json()
        except aiohttp.ContentTypeError as exc:
            raise MainBotMediaError(f"invalid Telegram response HTTP {response.status}") from exc
        if not isinstance(payload, dict):
            raise MainBotMediaError("invalid Telegram response payload")
        return payload

    def _require_session(self) -> aiohttp.ClientSession:
        if self._session is None:
            raise RuntimeError("MainBotMediaService is not started")
        return self._session
