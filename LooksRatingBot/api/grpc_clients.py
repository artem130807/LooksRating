from __future__ import annotations

from dataclasses import dataclass

import grpc

from grpc_gen import debited_sparks_pb2
from grpc_gen import debited_sparks_pb2_grpc
from grpc_gen import rollback_debited_sparks_pb2
from grpc_gen import rollback_debited_sparks_pb2_grpc


@dataclass(frozen=True)
class SparksGrpcResponse:
    success: bool
    message: str


class LooksRatingSparksGrpcClient:
    def __init__(self, address: str, timeout: float = 30.0):
        self._address = address
        self._timeout = timeout

    def debited_sparks(self, telegram_id: int, stars_count: int) -> SparksGrpcResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(("grpc.enable_http_proxy", 0),),
        )
        try:
            stub = debited_sparks_pb2_grpc.DebitedSparksServiceStub(channel)
            response = stub.DebitedSparks(
                debited_sparks_pb2.DebitedSparksRequest(
                    telegram_id=telegram_id,
                    sparks_count=stars_count,
                ),
                timeout=self._timeout,
            )
            return SparksGrpcResponse(success=bool(response.success), message=response.message or "")
        finally:
            channel.close()

    def rollback_debited_sparks(
        self,
        telegram_id: int,
        stars_count: int,
        *,
        reason: str = "gift_delivery_failed",
    ) -> SparksGrpcResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(("grpc.enable_http_proxy", 0),),
        )
        try:
            stub = rollback_debited_sparks_pb2_grpc.RollBackDebitedSparksServiceStub(channel)
            response = stub.RollBackDebitedSparks(
                rollback_debited_sparks_pb2.RollBackDebitedSparksRequest(
                    telegram_id=telegram_id,
                    stars_count=stars_count,
                    reason=reason,
                ),
                timeout=self._timeout,
            )
            return SparksGrpcResponse(success=bool(response.success), message=response.message or "")
        finally:
            channel.close()
