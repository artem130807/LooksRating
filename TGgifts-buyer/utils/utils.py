import asyncio
from datetime import datetime

from pyrogram import Client
from pyrogram.errors.exceptions import RPCError
from pytz.tzinfo import BaseTzInfo

import config


def time(timezone: BaseTzInfo) -> str:
    return datetime.now().astimezone(timezone).strftime("%d.%m.%y :: %H:%M:%S")


async def buyer(app: Client, chat_id: int, star_gift_id: int, hide_my_name: bool = config.HIDE_SENDER_NAME) -> None:
    locale = config.locale
    from src.notifications import notifications
    try:
        user = await app.get_chat(chat_id)
        username = user.username if user.username else ""

        recipient_info = (
            f"@{username.strip()}"
            if username
            else f"{chat_id}" if str(chat_id)[0].isdigit()
            else f"@{str(chat_id).strip()}"
        )

        num = config.NUM_GIFTS
        for i in range(num):
            await app.send_star_gift(
                chat_id=chat_id,
                star_gift_id=star_gift_id,
                hide_my_name=hide_my_name
            )

            print(f"\033[93m[ ★ ]\033[0m {locale.gift_sent.format(i + 1, num, star_gift_id, recipient_info)}\n")

            if config.GIFT_DELAY > 0:
                await notifications(app, star_gift_id, user_id=chat_id, username=username,
                                    current_gift=i + 1, total_gifts=num)

            if i < num - 1 and config.GIFT_DELAY > 0:
                await asyncio.sleep(config.GIFT_DELAY)

    except RPCError as ex:
        num = config.NUM_GIFTS
        error_message = f"<pre>{str(ex)}</pre>"
        if 'BALANCE_TOO_LOW' in str(ex) or '400 BALANCE_TOO_LOW' in str(ex):
            print(f"\n\033[91m[ ERROR ]\033[0m {locale.low_balance}\n")
            await notifications(app, star_gift_id, balance_error=True, total_gifts=num)
        elif 'STARGIFT_USAGE_LIMITED' in str(ex):
            print(f"\033[91m[ ERROR ]\033[0m {locale.out_of_stock.format(star_gift_id)}\n")
            await notifications(app, star_gift_id, usage_limited=True, total_gifts=num)
        elif 'PEER_ID_INVALID' in str(ex):
            print(
                f"\n\033[91m[ ERROR ]\033[0m {locale.peer_id}\n"
            )
            await notifications(app, star_gift_id, peer_id_error=True, total_gifts=num)
        else:
            print(
                f"\n\033[91m[ ERROR ]\033[0m {locale.gift_send_error.format(star_gift_id, chat_id)}\n{str(ex)}\n"
            )
            await notifications(
                app,
                star_gift_id,
                error_message=error_message,
                total_gifts=num
            )
