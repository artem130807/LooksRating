from __future__ import annotations

import asyncio
import logging
from datetime import datetime, timezone

from aiogram import Bot
from aiogram.exceptions import TelegramBadRequest, TelegramForbiddenError

from api.client import ApiError, TicketApiClient
from bot.html_escape import escape_html

logger = logging.getLogger(__name__)

_TELEGRAM_MESSAGE_LIMIT = 4096
_SEVERITY_EMOJI = {
    "critical": "🔴",
    "warning": "🟡",
}


class AlertDeliveryService:
    def __init__(self, api: TicketApiClient, bot: Bot, interval_seconds: int = 30) -> None:
        self._api = api
        self._bot = bot
        self._interval_seconds = max(10, interval_seconds)
        self._task: asyncio.Task | None = None
        self._stop_event = asyncio.Event()

    async def start(self) -> None:
        if self._task and not self._task.done():
            return
        self._stop_event.clear()
        self._task = asyncio.create_task(self._run(), name="alert-delivery-loop")

    async def stop(self) -> None:
        self._stop_event.set()
        if self._task:
            self._task.cancel()
            try:
                await self._task
            except asyncio.CancelledError:
                pass
            self._task = None

    async def _run(self) -> None:
        while not self._stop_event.is_set():
            try:
                await self._tick()
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Alert delivery tick failed")
            try:
                await asyncio.wait_for(self._stop_event.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                continue

    async def _tick(self) -> None:
        try:
            payload = await self._api.monitoring_pending_alerts()
        except ApiError as exc:
            logger.warning("Pending alerts API error: %s (%s)", exc.message, exc.status)
            return

        alerts = payload.get("alerts") or []
        recipients = [int(item) for item in (payload.get("recipients") or []) if int(item) > 0]
        if not alerts or not recipients:
            return

        for alert in alerts:
            alert_id = int(alert.get("id") or 0)
            if alert_id <= 0:
                continue
            text = format_alert_message(alert)
            delivered_to: list[int] = []
            failed_to: list[int] = []
            for chat_id in recipients:
                try:
                    await self._bot.send_message(chat_id, text)
                    delivered_to.append(chat_id)
                except TelegramForbiddenError:
                    logger.warning("Alert delivery blocked for chat_id=%s", chat_id)
                    failed_to.append(chat_id)
                except TelegramBadRequest as exc:
                    logger.warning("Alert delivery rejected for chat_id=%s: %s", chat_id, exc)
                    failed_to.append(chat_id)
                except Exception:
                    logger.exception("Failed to send alert %s to %s", alert_id, chat_id)
                    failed_to.append(chat_id)

            if len(delivered_to) != len(recipients):
                logger.warning(
                    "Alert %s delivered to %s/%s admins; failed=%s — ack skipped",
                    alert_id,
                    len(delivered_to),
                    len(recipients),
                    failed_to,
                )
                continue

            try:
                await self._api.monitoring_ack_alert(alert_id)
            except ApiError as exc:
                logger.warning("Alert ack failed for %s: %s", alert_id, exc.message)


def format_alert_message(alert: dict) -> str:
    severity = str(alert.get("severity") or "warning").lower()
    emoji = _SEVERITY_EMOJI.get(severity, "⚠️")
    title = escape_html(str(alert.get("title") or "Алерт"))
    body = escape_html(str(alert.get("body") or ""))
    first_seen = str(alert.get("firstSeenAt") or "")
    when = ""
    if first_seen:
        try:
            parsed = datetime.fromisoformat(first_seen.replace("Z", "+00:00"))
            when = parsed.astimezone(timezone.utc).strftime("%d.%m.%Y %H:%M UTC")
        except ValueError:
            when = escape_html(first_seen)
    lines = [f"{emoji} <b>{title}</b>"]
    if body:
        lines.append(body)
    if when:
        lines.append(f"<i>{when}</i>")
    text = "\n".join(lines)
    if len(text) <= _TELEGRAM_MESSAGE_LIMIT:
        return text
    overflow = len(text) - _TELEGRAM_MESSAGE_LIMIT + 3
    if body and len(body) > overflow:
        lines = [f"{emoji} <b>{title}</b>", body[:-overflow] + "..."]
        if when:
            lines.append(f"<i>{when}</i>")
        return "\n".join(lines)
    return text[: _TELEGRAM_MESSAGE_LIMIT - 3] + "..."
