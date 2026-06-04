from pyrogram import Client

import config


async def notifications(app: Client, star_gift_id: int, gift_price: float = None,
                        user_id: int = None, username: str = None, balance_error: bool = False,
                        error_message: str = None, non_limited_error: bool = False,
                        current_gift: int = None, usage_limited: bool = False, peer_id_error: bool = False,
                        total_gifts: int = None, gift_supply: int = None) -> None:
    num = config.NUM_GIFTS
    locale = config.locale

    if peer_id_error:
        message = locale.peer_id_error
    elif error_message:
        message = locale.error_message.format(error_message)
    elif balance_error:
        message = locale.balance_error.format(star_gift_id)
    elif usage_limited:
        message = locale.usage_limited.format(star_gift_id)
    elif non_limited_error:
        message = locale.non_limited_error.format(star_gift_id)
    elif gift_price:
        message = locale.gift_price.format(star_gift_id, gift_price, gift_supply or "N/A")
    elif current_gift:
        message = locale.success_message.format(current_gift, num, star_gift_id, '')

        if username:
            message += f"@{username} | <code>{user_id}</code>"
        elif str(user_id).isdigit():
            message += f'<a href="tg://user?id={user_id}">{user_id}</a>'
        else:
            message += f"@{user_id.strip()}"

    try:
        await app.send_message(config.CHANNEL_ID, message.strip())
    except Exception as ex:
        print(f"\n\033[91m[ ERROR ]\033[0m {locale.unexpected_error} {str(ex)}")
