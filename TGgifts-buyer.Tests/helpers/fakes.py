from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass
class FakePyrogramClient:
    gifts: list[dict[str, Any]] = field(
        default_factory=lambda: [
            {"id": 101, "price": 400},
            {"id": 102, "price": 300},
            {"id": 103, "price": 250},
            {"id": 104, "price": 200},
            {"id": 105, "price": 150},
        ]
    )
    get_users_calls: list[int] = field(default_factory=list)
    sent_messages: list[tuple[int, str]] = field(default_factory=list)
    buyer_calls: list[tuple[int, int]] = field(default_factory=list)
    get_star_gifts_error: Exception | None = None
    get_users_error: Exception | None = None

    async def get_star_gifts(self) -> list[dict[str, Any]]:
        if self.get_star_gifts_error:
            raise self.get_star_gifts_error
        return list(self.gifts)

    async def get_users(self, chat_id: int) -> object:
        if self.get_users_error:
            raise self.get_users_error
        self.get_users_calls.append(chat_id)
        return object()

    async def send_message(self, chat_id: int, text: str, **_: Any) -> None:
        self.sent_messages.append((chat_id, text))
