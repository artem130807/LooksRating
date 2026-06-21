from __future__ import annotations

from dataclasses import dataclass

import grpc

from api.grpc_auth import build_grpc_metadata
from bot.withdrawal_views import OUTPUT_STATUS_CANCELLED, OUTPUT_STATUS_CONFIRMED
from grpc_gen import get_writing_off_sparks_pb2
from grpc_gen import get_writing_off_sparks_pb2_grpc
from grpc_gen import get_writings_off_sparks_pb2
from grpc_gen import get_writings_off_sparks_pb2_grpc
from grpc_gen import list_writing_off_sparks_cities_pb2
from grpc_gen import list_writing_off_sparks_cities_pb2_grpc
from grpc_gen import update_status_writing_off_sparks_pb2
from grpc_gen import update_status_writing_off_sparks_pb2_grpc


@dataclass(frozen=True)
class WritingOffSparksItem:
    id: str
    status: int
    user_id: str
    telegram_id: int
    city: str
    sparks_count: int
    stars: int
    created_at_unix_seconds: int


@dataclass(frozen=True)
class WritingsOffSparksPage:
    success: bool
    message: str
    items: list[WritingOffSparksItem]
    total_count: int
    page: int
    page_size: int
    has_next_page: bool


@dataclass(frozen=True)
class WritingOffSparksDetail:
    success: bool
    message: str
    item: WritingOffSparksItem | None


@dataclass(frozen=True)
class UpdateStatusResult:
    success: bool
    message: str


def _map_item(proto_item) -> WritingOffSparksItem:
    return WritingOffSparksItem(
        id=proto_item.id,
        status=int(proto_item.status),
        user_id=proto_item.user_id,
        telegram_id=int(proto_item.telegram_id),
        city=proto_item.city,
        sparks_count=int(proto_item.sparks_count),
        stars=int(proto_item.stars),
        created_at_unix_seconds=int(proto_item.created_at_unix_seconds),
    )


class WritingOffSparksGrpcClient:
    def __init__(self, address: str, timeout: float = 30.0, *, api_key: str = ""):
        self._address = address
        self._timeout = timeout
        self._metadata = build_grpc_metadata(api_key)

    def _channel(self):
        return grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )

    def list_by_city(self, city: str, page: int, page_size: int) -> WritingsOffSparksPage:
        channel = self._channel()
        try:
            stub = get_writings_off_sparks_pb2_grpc.GetWritingsOffSparksServiceStub(channel)
            response = stub.GetWritingsOffSparks(
                get_writings_off_sparks_pb2.GetWritingsOffSparksRequest(
                    city=city,
                    page=page,
                    page_size=page_size,
                ),
                timeout=self._timeout,
                metadata=self._metadata,
            )
            items = [_map_item(item) for item in response.items]
            return WritingsOffSparksPage(
                success=bool(response.success),
                message=response.message or "",
                items=items,
                total_count=int(response.total_count),
                page=int(response.page or page),
                page_size=int(response.page_size or page_size),
                has_next_page=bool(response.has_next_page),
            )
        except grpc.RpcError as exc:
            return WritingsOffSparksPage(
                success=False,
                message=str(exc),
                items=[],
                total_count=0,
                page=page,
                page_size=page_size,
                has_next_page=False,
            )
        finally:
            channel.close()

    def get_by_id(self, writing_off_sparks_id: str) -> WritingOffSparksDetail:
        channel = self._channel()
        try:
            stub = get_writing_off_sparks_pb2_grpc.GetWritingOffSparksServiceStub(channel)
            response = stub.GetWritingOffSparks(
                get_writing_off_sparks_pb2.GetWritingOffSparksRequest(
                    writing_off_sparks_id=writing_off_sparks_id,
                ),
                timeout=self._timeout,
                metadata=self._metadata,
            )
            item = _map_item(response.item) if response.item and response.item.id else None
            return WritingOffSparksDetail(
                success=bool(response.success),
                message=response.message or "",
                item=item,
            )
        except grpc.RpcError as exc:
            return WritingOffSparksDetail(success=False, message=str(exc), item=None)
        finally:
            channel.close()

    def list_pending_cities(self) -> tuple[bool, str, list[str]]:
        channel = self._channel()
        try:
            stub = list_writing_off_sparks_cities_pb2_grpc.ListWritingOffSparksCitiesServiceStub(
                channel
            )
            response = stub.ListWritingOffSparksCities(
                list_writing_off_sparks_cities_pb2.ListWritingOffSparksCitiesRequest(),
                timeout=self._timeout,
                metadata=self._metadata,
            )
            return bool(response.success), response.message or "", list(response.cities)
        except grpc.RpcError as exc:
            return False, str(exc), []
        finally:
            channel.close()

    def mark_confirmed(self, writing_off_sparks_id: str) -> UpdateStatusResult:
        return self._update_status(writing_off_sparks_id, OUTPUT_STATUS_CONFIRMED)

    def mark_cancelled(self, writing_off_sparks_id: str) -> UpdateStatusResult:
        return self._update_status(writing_off_sparks_id, OUTPUT_STATUS_CANCELLED)

    def _update_status(self, writing_off_sparks_id: str, status: int) -> UpdateStatusResult:
        channel = self._channel()
        try:
            stub = update_status_writing_off_sparks_pb2_grpc.UpdateStatusWritingOffSparksServiceStub(
                channel
            )
            response = stub.UpdateStatusWritingOffSparks(
                update_status_writing_off_sparks_pb2.UpdateStatusWritingOffSparksRequest(
                    writing_off_sparks_id=writing_off_sparks_id,
                    status=status,
                ),
                timeout=self._timeout,
                metadata=self._metadata,
            )
            return UpdateStatusResult(
                success=bool(response.success),
                message=response.message or "",
            )
        except grpc.RpcError as exc:
            return UpdateStatusResult(success=False, message=str(exc))
        finally:
            channel.close()
