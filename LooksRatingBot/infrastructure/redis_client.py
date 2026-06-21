from __future__ import annotations

import logging

import redis.asyncio as redis

logger = logging.getLogger(__name__)


async def create_redis_client(redis_url: str) -> redis.Redis:
    client = redis.from_url(
        redis_url,
        decode_responses=True,
        health_check_interval=30,
        socket_connect_timeout=5,
        socket_timeout=5,
        retry_on_timeout=True,
    )
    await client.ping()
    logger.info("Redis connected: %s", redis_url)
    return client


async def close_redis_client(client: redis.Redis | None) -> None:
    if client is None:
        return
    await client.aclose()
