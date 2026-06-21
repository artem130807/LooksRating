from __future__ import annotations

from dataclasses import dataclass

import grpc

from grpc_gen import create_writing_off_sparks_pb2
from grpc_gen import create_writing_off_sparks_pb2_grpc


@dataclass(frozen=True)
class WritingOffSparksResponse:
    success: bool
    message: str


from api.grpc_auth import build_grpc_metadata


class WritingOffSparksGrpcClient:
    """LooksRating API client for persisting sparks-to-stars exchange."""

    def __init__(self, address: str, timeout: float = 30.0, *, api_key: str = ""):
        self._address = address
        self._timeout = timeout
        self._metadata = build_grpc_metadata(api_key)

    def create_writing_off_sparks(
        self,
        telegram_id: int,
        sparks_count: int,
        stars_count: int,
        *,
        idempotency_key: str,
    ) -> WritingOffSparksResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(
                ("grpc.enable_http_proxy", 0),
                ("grpc.http2.scheme", "http"),
            ),
        )
        try:
            stub = create_writing_off_sparks_pb2_grpc.CreateWritingOffSparksServiceStub(channel)
            response = stub.CreateWritingOffSparks(
                create_writing_off_sparks_pb2.CreateWritingOffSparksRequest(
                    telegram_id=telegram_id,
                    sparks_count=sparks_count,
                    stars_count=stars_count,
                    key=idempotency_key,
                ),
                timeout=self._timeout,
                metadata=self._metadata,
            )
            return WritingOffSparksResponse(
                success=bool(response.success),
                message=response.message or "",
            )
        except grpc.RpcError as exc:
            return WritingOffSparksResponse(success=False, message=str(exc))
        finally:
            channel.close()
