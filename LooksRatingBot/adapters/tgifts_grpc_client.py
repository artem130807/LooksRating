from __future__ import annotations

from dataclasses import dataclass

import grpc

from grpc_gen import send_gift_pb2
from grpc_gen import send_gift_pb2_grpc


@dataclass(frozen=True)
class GiftSendResponse:
    success: bool
    message: str


class TGiftsGrpcClient:
    def __init__(self, address: str, timeout: float = 120.0):
        self._address = address
        self._timeout = timeout

    def send_gift(self, recipient_telegram_id: int, star_price: int) -> GiftSendResponse:
        channel = grpc.insecure_channel(
            self._address,
            options=(("grpc.enable_http_proxy", 0),),
        )
        try:
            stub = send_gift_pb2_grpc.TelegramGiftSenderServiceStub(channel)
            response = stub.SendGift(
                send_gift_pb2.SendGiftRequest(
                    recipient_telegram_id=recipient_telegram_id,
                    star_price=star_price,
                ),
                timeout=self._timeout,
            )
            return GiftSendResponse(success=bool(response.success), message=response.message or "")
        except grpc.RpcError as exc:
            return GiftSendResponse(success=False, message=str(exc))
        finally:
            channel.close()
