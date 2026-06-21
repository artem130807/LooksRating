from __future__ import annotations

import logging

from aiohttp import web

from bot.writing_off_sparks_user_notifier import WritingOffSparksUserNotifier

logger = logging.getLogger(__name__)

INTERNAL_NOTIFY_HEADER = "X-Internal-Notify-Key"


def create_internal_notify_app(
    notifier: WritingOffSparksUserNotifier,
    api_key: str,
) -> web.Application:
    app = web.Application()

    async def writing_off_sparks_confirmed(request: web.Request) -> web.Response:
        if (request.headers.get(INTERNAL_NOTIFY_HEADER) or "").strip() != api_key:
            return web.json_response({"success": False, "message": "Unauthorized"}, status=401)

        try:
            payload = await request.json()
        except Exception:
            return web.json_response({"success": False, "message": "Invalid JSON"}, status=400)

        telegram_id = int(payload.get("telegram_id") or 0)
        stars = int(payload.get("stars") or 0)
        if telegram_id <= 0 or stars <= 0:
            return web.json_response(
                {"success": False, "message": "telegram_id and stars are required"},
                status=400,
            )

        delivered = await notifier.notify_confirmed(telegram_id=telegram_id, stars=stars)
        if not delivered:
            return web.json_response(
                {"success": False, "message": "Failed to deliver notification"},
                status=502,
            )

        return web.json_response({"success": True, "message": "Notification delivered"})

    async def writing_off_sparks_cancelled(request: web.Request) -> web.Response:
        if (request.headers.get(INTERNAL_NOTIFY_HEADER) or "").strip() != api_key:
            return web.json_response({"success": False, "message": "Unauthorized"}, status=401)

        try:
            payload = await request.json()
        except Exception:
            return web.json_response({"success": False, "message": "Invalid JSON"}, status=400)

        telegram_id = int(payload.get("telegram_id") or 0)
        stars = int(payload.get("stars") or 0)
        sparks_count = int(payload.get("sparks_count") or 0)
        if telegram_id <= 0 or stars <= 0 or sparks_count <= 0:
            return web.json_response(
                {
                    "success": False,
                    "message": "telegram_id, stars and sparks_count are required",
                },
                status=400,
            )

        delivered = await notifier.notify_cancelled(
            telegram_id=telegram_id,
            stars=stars,
            sparks=sparks_count,
        )
        if not delivered:
            return web.json_response(
                {"success": False, "message": "Failed to deliver notification"},
                status=502,
            )

        return web.json_response({"success": True, "message": "Notification delivered"})

    app.router.add_post(
        "/internal/notifications/writing-off-sparks-confirmed",
        writing_off_sparks_confirmed,
    )
    app.router.add_post(
        "/internal/notifications/writing-off-sparks-cancelled",
        writing_off_sparks_cancelled,
    )
    return app


async def start_internal_notify_server(
    notifier: WritingOffSparksUserNotifier,
    *,
    host: str,
    port: int,
    api_key: str,
) -> web.AppRunner:
    app = create_internal_notify_app(notifier, api_key)
    runner = web.AppRunner(app)
    await runner.setup()
    site = web.TCPSite(runner, host=host, port=port)
    await site.start()
    logger.info("Internal notify server listening on %s:%s", host, port)
    return runner
