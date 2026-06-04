from pyrogram import Client


async def resolve_gift_ids_by_price(app: Client, prices: set[int]) -> dict[int, int]:
    gifts = await app.get_star_gifts()
    resolved: dict[int, int] = {}

    for gift in gifts:
        gift_id = gift.get("id")
        gift_price = gift.get("price")
        if gift_id is None or gift_price is None:
            continue
        price = int(gift_price)
        if price not in prices or price in resolved:
            continue
        resolved[price] = int(gift_id)

    return resolved
