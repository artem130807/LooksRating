from dataclasses import dataclass


@dataclass(frozen=True, slots=True)
class VipTopProfile:
    telegram_id: int
    city: str
    rating: float
    rating_count: int
    age: int
    gender: int
    created_at_unix: int


@dataclass(frozen=True, slots=True)
class VipGiftRecipient:
    telegram_id: int
    place: int
    star_price: int
