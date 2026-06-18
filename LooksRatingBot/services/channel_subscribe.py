from __future__ import annotations

from aiogram import Bot
from aiogram.enums import ChatMemberStatus

from api.grpc_clients import ChannelSubscribeBonusResponse, LooksRatingGrpcClient
from bot import callbacks, texts

_SUBSCRIBED_STATUSES = frozenset(
    {
        ChatMemberStatus.CREATOR,
        ChatMemberStatus.ADMINISTRATOR,
        ChatMemberStatus.MEMBER,
        ChatMemberStatus.RESTRICTED,
    }
)


def resolve_subscribe_confirm_message(
    check_response: ChannelSubscribeBonusResponse,
    credit_response: ChannelSubscribeBonusResponse | None,
    *,
    is_channel_member: bool,
) -> str:
    if check_response.status == callbacks.CHANNEL_SUBSCRIBE_STATUS_ALREADY_CREDITED:
        return texts.CHANNEL_SUBSCRIBE_ALREADY_CREDITED

    if check_response.status == callbacks.CHANNEL_SUBSCRIBE_STATUS_USER_NOT_FOUND:
        return texts.CHANNEL_SUBSCRIBE_USER_NOT_FOUND

    if not is_channel_member:
        return texts.CHANNEL_SUBSCRIBE_NOT_MEMBER

    if credit_response is None:
        return texts.CHANNEL_SUBSCRIBE_FAILED

    if credit_response.status == callbacks.CHANNEL_SUBSCRIBE_STATUS_CREDITED:
        return texts.CHANNEL_SUBSCRIBE_SUCCESS

    if credit_response.status == callbacks.CHANNEL_SUBSCRIBE_STATUS_ALREADY_CREDITED:
        return texts.CHANNEL_SUBSCRIBE_ALREADY_CREDITED

    if credit_response.message:
        return credit_response.message

    return texts.CHANNEL_SUBSCRIBE_FAILED


async def process_subscribe_confirm(
    grpc_client: LooksRatingGrpcClient,
    bot: Bot,
    telegram_id: int,
    channel_username: str,
) -> str:
    check_response = grpc_client.channel_subscribe_bonus(telegram_id, credit=False)

    if check_response.status in {
        callbacks.CHANNEL_SUBSCRIBE_STATUS_ALREADY_CREDITED,
        callbacks.CHANNEL_SUBSCRIBE_STATUS_USER_NOT_FOUND,
    }:
        return resolve_subscribe_confirm_message(
            check_response,
            None,
            is_channel_member=False,
        )

    member = await bot.get_chat_member(
        chat_id=f"@{channel_username.lstrip('@')}",
        user_id=telegram_id,
    )
    is_member = member.status in _SUBSCRIBED_STATUSES

    if not is_member:
        return resolve_subscribe_confirm_message(
            check_response,
            None,
            is_channel_member=False,
        )

    credit_response = grpc_client.channel_subscribe_bonus(telegram_id, credit=True)
    return resolve_subscribe_confirm_message(
        check_response,
        credit_response,
        is_channel_member=True,
    )
