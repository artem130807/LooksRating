from __future__ import annotations

import os

import aiohttp


def normalize_telegram_proxy(proxy: str | None) -> str | None:
    """Map localhost proxy URLs to the Docker host when running in a container."""
    if not proxy:
        return None

    in_docker = os.path.exists("/.dockerenv") or os.getenv("DOCKER", "").strip().lower() in {
        "1",
        "true",
        "yes",
    }
    if not in_docker:
        return proxy

    normalized = proxy
    for host in ("127.0.0.1", "localhost"):
        normalized = normalized.replace(f"//{host}:", "//host.docker.internal:")
        normalized = normalized.replace(f"@{host}:", "@host.docker.internal:")
    return normalized


def create_client_session(
    *,
    proxy: str | None = None,
    timeout_seconds: float = 60.0,
) -> aiohttp.ClientSession:
    timeout = aiohttp.ClientTimeout(total=timeout_seconds)
    proxy = normalize_telegram_proxy(proxy)
    if proxy:
        from aiohttp_socks import ProxyConnector

        connector = ProxyConnector.from_url(proxy)
        return aiohttp.ClientSession(connector=connector, timeout=timeout)
    return aiohttp.ClientSession(timeout=timeout)
