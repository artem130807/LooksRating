from __future__ import annotations

from dataclasses import dataclass

import grpc

from grpc_gen import current_sparks_for_user_pb2
from grpc_gen import current_sparks_for_user_pb2_grpc
from grpc_gen import debited_sparks_pb2
from grpc_gen import debited_sparks_pb2_grpc
from grpc_gen import get_unactive_users_pb2
from grpc_gen import get_unactive_users_pb2_grpc
from grpc_gen import get_users_for_message_pb2
from grpc_gen import get_users_for_message_pb2_grpc
from grpc_gen import rollback_debited_sparks_pb2
from grpc_gen import rollback_debited_sparks_pb2_grpc


@dataclass(frozen=True)
class SparksGrpcResponse:
    success: bool
    message: str


@dataclass(frozen=True)
class UsersForMessagePage:
    telegram_ids: list[int]
    total_count: int
    page: int
    page_size: int
    has_next_page: bool


@dataclass(frozen=True)
class ChannelSubscribeBonusResponse:
    success: bool
    message: str
    status: str


from api.grpc_auth import build_grpc_metadata


class LooksRatingSparksGrpcClient:
    def __init__(self, address: str, timeout: float = 30.0, *, api_key: str = ""):
        self._address = address
        self._timeout = timeout
        self._metadata = build_grpc_metadata(api_key)

    def debited_sparks(
        self,
        telegram_id: int,
        stars_count: int,
        *,
        idempotency_key: str | None = None,
    ) -> SparksGrpcResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )
        try:
            stub = debited_sparks_pb2_grpc.DebitedSparksServiceStub(channel)
            request = debited_sparks_pb2.DebitedSparksRequest(
                telegram_id=telegram_id,
                sparks_count=stars_count,
            )
            if idempotency_key is not None:
                request.key = idempotency_key
            response = stub.DebitedSparks(
                request,
                timeout=self._timeout,
                metadata=self._metadata,
            )
            return SparksGrpcResponse(success=bool(response.success), message=response.message or "")
        except grpc.RpcError as exc:
            return SparksGrpcResponse(success=False, message=str(exc))
        finally:
            channel.close()

    def rollback_debited_sparks(
        self,
        telegram_id: int,
        stars_count: int,
        *,
        reason: str = "gift_delivery_failed",
        idempotency_key: str | None = None,
    ) -> SparksGrpcResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )
        try:
            stub = rollback_debited_sparks_pb2_grpc.RollBackDebitedSparksServiceStub(channel)
            request = rollback_debited_sparks_pb2.RollBackDebitedSparksRequest(
                telegram_id=telegram_id,
                stars_count=stars_count,
                reason=reason,
            )
            if idempotency_key is not None:
                request.key = idempotency_key
            response = stub.RollBackDebitedSparks(
                request,
                timeout=self._timeout,
                metadata=self._metadata,
            )
            return SparksGrpcResponse(success=bool(response.success), message=response.message or "")
        except grpc.RpcError as exc:
            return SparksGrpcResponse(success=False, message=str(exc))
        finally:
            channel.close()


class LooksRatingGrpcClient:
    def __init__(self, address: str, timeout: float = 30.0):
        self._address = address
        self._timeout = timeout

    def get_users_for_message(
        self,
        page: int,
        page_size: int,
        *,
        only_unsubscribed_channel: bool = False,
    ) -> UsersForMessagePage:
        channel = grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )
        try:
            stub = get_users_for_message_pb2_grpc.GetUsersForMessageServiceStub(channel)
            response = stub.GetUsersForMessage(
                get_users_for_message_pb2.GetUsersForMessageRequest(
                    page=page,
                    page_size=page_size,
                    only_unsubscribed_channel=only_unsubscribed_channel,
                ),
                timeout=self._timeout,
            )
            return UsersForMessagePage(
                telegram_ids=[int(item) for item in response.telegram_ids],
                total_count=int(response.total_count),
                page=int(response.page),
                page_size=int(response.page_size),
                has_next_page=bool(response.has_next_page),
            )
        finally:
            channel.close()

    def channel_subscribe_bonus(
        self,
        telegram_id: int,
        *,
        credit: bool,
    ) -> ChannelSubscribeBonusResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )
        try:
            stub = current_sparks_for_user_pb2_grpc.CurrentSparksForUserServiceStub(channel)
            response = stub.CurrentSparksForUser(
                current_sparks_for_user_pb2.CurrentSparksForUserRequest(
                    telegram_id=telegram_id,
                    credit=credit,
                ),
                timeout=self._timeout,
            )
            status_name = current_sparks_for_user_pb2.ChannelSubscribeBonusStatus.Name(response.status)
            return ChannelSubscribeBonusResponse(
                success=bool(response.success),
                message=response.message or "",
                status=status_name,
            )
        finally:
            channel.close()

    def get_unactive_users(self) -> list[int]:
        channel = grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )
        try:
            stub = get_unactive_users_pb2_grpc.GetUnActiveUsersServiceStub(channel)
            response = stub.GetUnActiveUsers(
                get_unactive_users_pb2.GetUnActiveUsersRequest(),
                timeout=self._timeout,
            )
            return [int(item) for item in response.telegram_ids if int(item) > 0]
        finally:
            channel.close()
