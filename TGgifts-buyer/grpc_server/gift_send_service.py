from concurrent import futures

import grpc
from pyrogram import Client

from grpc_gen import send_gift_pb2
from grpc_gen import send_gift_pb2_grpc
from services.single_gift_dispatch import send_gift_to_user


class TelegramGiftSenderServicer(send_gift_pb2_grpc.TelegramGiftSenderServiceServicer):
    def __init__(self, app: Client):
        self._app = app

    async def SendGift(self, request, context):
        success, message = await send_gift_to_user(
            self._app,
            int(request.recipient_telegram_id),
            int(request.star_price),
        )
        return send_gift_pb2.SendGiftResponse(success=success, message=message)


async def start_gift_grpc_server(app: Client, host: str, port: int) -> grpc.aio.Server:
    server = grpc.aio.server(futures.ThreadPoolExecutor(max_workers=4))
    send_gift_pb2_grpc.add_TelegramGiftSenderServiceServicer_to_server(
        TelegramGiftSenderServicer(app),
        server,
    )
    server.add_insecure_port(f"{host}:{port}")
    await server.start()
    return server
