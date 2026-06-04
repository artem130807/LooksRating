import asyncio

from pyrogram import Client
from pyrogram.errors.exceptions import RPCError

import config
from services.vip_top_models import VipGiftRecipient
from utils.utils import buyer


async def dispatch_ranked_gifts(
    app: Client,
    recipients: list[VipGiftRecipient],
    gift_ids_by_price: dict[int, int],
    *,
    send_intro_message: bool = False,
) -> tuple[int, int]:
    if not recipients:
        print("\033[93m[ VIP JOB ]\033[0m Список получателей пуст — рассылка пропущена.")
        return 0, 0

    locale = config.locale
    success = 0
    failed = 0

    for recipient in recipients:
        gift_id = gift_ids_by_price.get(recipient.star_price)
        if gift_id is None:
            failed += 1
            print(
                f"\033[91m[ VIP JOB ]\033[0m Не найден подарок за {recipient.star_price}★ "
                f"(место {recipient.place}, id {recipient.telegram_id})"
            )
            continue

        chat_id = recipient.telegram_id
        try:
            if send_intro_message:
                await app.send_message(
                    chat_id,
                    "👋 Подарок от LooksRating за попадание в топ VIP.\n"
                    "Сообщение можно проигнорировать.",
                    disable_web_page_preview=True,
                )
            await app.get_users(chat_id)
            await buyer(app, chat_id, int(gift_id))
            success += 1
            await asyncio.sleep(max(config.GIFT_DELAY, 1.0))
        except RPCError as ex:
            failed += 1
            print(
                f"\n\033[91m[ ERROR ]\033[0m {locale.purchase_error.format(gift_id, chat_id)}\n{str(ex)}\n"
            )

    return success, failed
