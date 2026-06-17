import asyncio

from pyrogram import Client

import config
from src.notifications import notifications
from utils.utils import buyer

sent_gift_ids = set()


async def new_callback(app: Client, star_gift_raw: dict) -> None:
    gift_price = star_gift_raw.get("price", 0)
    gift_supply = star_gift_raw.get("total_amount", 0)
    locale = config.locale

    if (gift_price <= config.MIN_GIFT_PRICE or gift_price >= config.MAX_GIFT_PRICE or (config.GIFT_SUPPLY is not None and gift_supply > config.GIFT_SUPPLY)):
        print(
            f"\033[91m[ ! ]\033[0m {locale.gift_expensive.format(star_gift_raw['id'], gift_price, gift_supply)}\n"
        )
        await notifications(app, star_gift_raw['id'], gift_price=gift_price, gift_supply=gift_supply)
        return

    if star_gift_raw.get("is_limited", False):
        if star_gift_raw["id"] in sent_gift_ids:
            return
        sent_gift_ids.add(star_gift_raw["id"])

    elif config.PURCHASE_NON_LIMITED_GIFTS and gift_price <= config.MAX_GIFT_PRICE:
        if star_gift_raw["id"] not in sent_gift_ids:
            sent_gift_ids.add(star_gift_raw["id"])
    else:
        print(
            f"\033[91m[ ! ]\033[0m {locale.non_limited_gift.format(star_gift_raw['id'])}\n"
        )
        await notifications(app, star_gift_raw['id'], non_limited_error=True)
        return

    for i, chat_id in enumerate(config.TARGET_USER_IDS):
        await buyer(app, chat_id, star_gift_raw["id"])
        if i < len(config.TARGET_USER_IDS) - 1:
            await asyncio.sleep(config.GIFT_DELAY)


async def update_callback(new_gift_raw: dict) -> None:
    if "message_id" not in new_gift_raw:
        return

    message_id = new_gift_raw["message_id"]
