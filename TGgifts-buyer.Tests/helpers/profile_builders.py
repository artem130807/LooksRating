from __future__ import annotations

from services.vip_top_models import VipTopProfile


def make_profile(
    *,
    telegram_id: int = 1_001,
    city: str = "moscow",
    rating: float = 8.0,
    rating_count: int = 10,
    age: int = 25,
    gender: int = 1,
    created_at_unix: int = 1_000,
) -> VipTopProfile:
    return VipTopProfile(
        telegram_id=telegram_id,
        city=city,
        rating=rating,
        rating_count=rating_count,
        age=age,
        gender=gender,
        created_at_unix=created_at_unix,
    )


def make_category_profiles(
    count: int,
    *,
    base_telegram_id: int = 10_000,
    city: str = "moscow",
    gender: int = 1,
    age: int = 25,
) -> list[VipTopProfile]:
    profiles: list[VipTopProfile] = []
    for index in range(count):
        profiles.append(
            make_profile(
                telegram_id=base_telegram_id + index,
                city=city,
                gender=gender,
                age=age,
                rating=9.5 - index * 0.1,
                rating_count=20 + index,
                created_at_unix=10_000 - index,
            )
        )
    return profiles
