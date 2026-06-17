import asyncio
import traceback

from pyrogram import Client
from pyrogram.errors.exceptions import RPCError
from pytz import timezone as _timezone

import config
from src.banner import title, info, cmd, get_locale
from src.callbacks import update_callback, new_callback
from grpc_server.gift_send_service import start_gift_grpc_server
from utils import utils
from utils.detector import detector
from utils.utils import buyer

app_info = info()
language, _ = get_locale(config.LANGUAGE)
cmd(app_info)
title(app_info, language)

sent_gift_ids = set()
timezone = _timezone(config.TIMEZONE)


async def _startup_gift_dispatch(app: Client) -> None:
    if not config.STARTUP_GIFT_DISPATCH:
        return

    config.init_target_user_ids()
    if not config.TARGET_USER_IDS:
        print("\033[93m[ INFO ]\033[0m STARTUP_GIFT_DISPATCH включён, но список получателей пуст.")
        return

    locale = config.locale
    for gift_id in config.GIFT_IDS:
        if gift_id in sent_gift_ids:
            continue
        for chat_id in config.TARGET_USER_IDS:
            try:
                await app.send_message(
                    chat_id,
                    "👋 Just a quick check-in! Feel free to ignore this message.\n\n"
                    "⭐Sent via <a href='https://github.com/bohd4nx/TGgifts-buyer'>Gifts Buyer</a>\n"
                    "🧑‍💻Developed by @B7XX7B (@GiftsTracker)",
                    disable_web_page_preview=True,
                )
                await app.get_users(chat_id)
                await buyer(app, chat_id, int(gift_id))
                await asyncio.sleep(5)
            except RPCError as ex:
                print(
                    f"\n\033[91m[ ERROR ]\033[0m {locale.purchase_error.format(gift_id, chat_id)}\n{str(ex)}\n"
                )
        sent_gift_ids.add(gift_id)


async def main() -> None:
    if not config.is_gift_grpc_mode() and not config.is_detector_mode():
        raise RuntimeError("Включите APP_MODE=gift_grpc, detector или both.")

    app = Client(name=config.SESSION, api_id=config.API_ID, api_hash=config.API_HASH)
    await app.start()

    gift_grpc_server = None
    if config.GIFT_GRPC_ENABLED:
        gift_grpc_server = await start_gift_grpc_server(
            app,
            config.GIFT_GRPC_HOST,
            config.GIFT_GRPC_PORT,
        )
        print(
            f"\033[94m[ INFO ]\033[0m Gift gRPC server started on "
            f"{config.GIFT_GRPC_HOST}:{config.GIFT_GRPC_PORT}"
        )

    if config.is_detector_mode():
        await _startup_gift_dispatch(app)
        await detector(app, new_callback, update_callback)
    elif config.is_gift_grpc_mode():
        print("\033[94m[ INFO ]\033[0m Режим gift_grpc: gRPC отправки подарков (Ctrl+C для выхода).")
        try:
            while True:
                await asyncio.sleep(3600)
        except asyncio.CancelledError:
            pass

    if gift_grpc_server is not None:
        await gift_grpc_server.stop(grace=5)

    await app.stop()


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        current_time = utils.time(timezone)
        print(f"\n\n\033[91m[ INFO ]\033[0m \033[1m{config.locale.terminated}\033[0m - {current_time}")
    except Exception:
        print(f"\n\n\033[91m[ ERROR ]\033[0m {config.locale.unexpected_error}")
        traceback.print_exc()
