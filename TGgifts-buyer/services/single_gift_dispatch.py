import asyncio

from pyrogram import Client
from pyrogram.errors.exceptions import RPCError

import config
from services.gift_price_resolver import resolve_gift_ids_by_price
from utils.utils import buyer


async def send_gift_to_user(app: Client, telegram_id: int, star_price: int) -> tuple[bool, str]:
    if telegram_id <= 0:
        return False, "Некорректный telegram_id"

    if star_price <= 0:
        return False, "Некорректная цена подарка в звёздах"

    gift_ids = await resolve_gift_ids_by_price(app, {star_price})
    gift_id = gift_ids.get(star_price)
    if gift_id is None:
        return False, f"Подарок за {star_price}★ не найден"

    try:
        await app.get_users(telegram_id)
        await buyer(app, telegram_id, int(gift_id))
        await asyncio.sleep(max(config.GIFT_DELAY, 1.0))
        return True, "Подарок отправлен"
    except RPCError as ex:
        return False, str(ex)
