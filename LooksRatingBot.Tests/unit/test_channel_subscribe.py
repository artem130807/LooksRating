from __future__ import annotations

import pytest
from unittest.mock import AsyncMock, MagicMock

from api.grpc_clients import ChannelSubscribeBonusResponse
from bot import callbacks, texts
from services.channel_subscribe import process_subscribe_confirm, resolve_subscribe_confirm_message


def test_resolve_subscribe_confirm_message_already_credited() -> None:
    check = ChannelSubscribeBonusResponse(
        success=True,
        message="",
        status=callbacks.CHANNEL_SUBSCRIBE_STATUS_ALREADY_CREDITED,
    )

    result = resolve_subscribe_confirm_message(check, None, is_channel_member=False)

    assert result == texts.CHANNEL_SUBSCRIBE_ALREADY_CREDITED


def test_resolve_subscribe_confirm_message_not_member() -> None:
    check = ChannelSubscribeBonusResponse(
        success=True,
        message="",
        status=callbacks.CHANNEL_SUBSCRIBE_STATUS_ELIGIBLE,
    )

    result = resolve_subscribe_confirm_message(check, None, is_channel_member=False)

    assert result == texts.CHANNEL_SUBSCRIBE_NOT_MEMBER


def test_resolve_subscribe_confirm_message_credited() -> None:
    check = ChannelSubscribeBonusResponse(
        success=True,
        message="",
        status=callbacks.CHANNEL_SUBSCRIBE_STATUS_ELIGIBLE,
    )
    credit = ChannelSubscribeBonusResponse(
        success=True,
        message="ok",
        status=callbacks.CHANNEL_SUBSCRIBE_STATUS_CREDITED,
    )

    result = resolve_subscribe_confirm_message(check, credit, is_channel_member=True)

    assert result == texts.CHANNEL_SUBSCRIBE_SUCCESS


@pytest.mark.asyncio
async def test_process_subscribe_confirm_when_already_credited_skips_chat_member() -> None:
    grpc_client = MagicMock()
    grpc_client.channel_subscribe_bonus.return_value = ChannelSubscribeBonusResponse(
        success=True,
        message="",
        status=callbacks.CHANNEL_SUBSCRIBE_STATUS_ALREADY_CREDITED,
    )
    bot = AsyncMock()

    result = await process_subscribe_confirm(grpc_client, bot, 12345, "LooksRatingBotOfficial")

    assert result == texts.CHANNEL_SUBSCRIBE_ALREADY_CREDITED
    bot.get_chat_member.assert_not_called()
    grpc_client.channel_subscribe_bonus.assert_called_once_with(12345, credit=False)


@pytest.mark.asyncio
async def test_process_subscribe_confirm_when_not_member_does_not_credit() -> None:
    grpc_client = MagicMock()
    grpc_client.channel_subscribe_bonus.return_value = ChannelSubscribeBonusResponse(
        success=True,
        message="",
        status=callbacks.CHANNEL_SUBSCRIBE_STATUS_ELIGIBLE,
    )
    bot = AsyncMock()
    member = MagicMock()
    member.status = "left"
    bot.get_chat_member.return_value = member

    result = await process_subscribe_confirm(grpc_client, bot, 12345, "LooksRatingBotOfficial")

    assert result == texts.CHANNEL_SUBSCRIBE_NOT_MEMBER
    assert grpc_client.channel_subscribe_bonus.call_count == 1


@pytest.mark.asyncio
async def test_process_subscribe_confirm_when_member_credits_bonus() -> None:
    grpc_client = MagicMock()
    grpc_client.channel_subscribe_bonus.side_effect = [
        ChannelSubscribeBonusResponse(
            success=True,
            message="",
            status=callbacks.CHANNEL_SUBSCRIBE_STATUS_ELIGIBLE,
        ),
        ChannelSubscribeBonusResponse(
            success=True,
            message="",
            status=callbacks.CHANNEL_SUBSCRIBE_STATUS_CREDITED,
        ),
    ]
    bot = AsyncMock()
    member = MagicMock()
    member.status = "member"
    bot.get_chat_member.return_value = member

    result = await process_subscribe_confirm(grpc_client, bot, 12345, "LooksRatingBotOfficial")

    assert result == texts.CHANNEL_SUBSCRIBE_SUCCESS
    assert grpc_client.channel_subscribe_bonus.call_args_list == [
        ((12345,), {"credit": False}),
        ((12345,), {"credit": True}),
    ]
