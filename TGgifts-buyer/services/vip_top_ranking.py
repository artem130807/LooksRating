from collections import defaultdict
from functools import cmp_to_key

from services.vip_top_models import VipGiftRecipient, VipTopProfile

PRIOR_MEAN = 8.0
PRIOR_WEIGHT = 5
UNRATED_SCORE = -1.0
GIFT_PLACES = 5
PLACE_STAR_PRICES = (400, 300, 250, 200, 150)

AGE_BRACKETS: tuple[tuple[int, ...], ...] = (
    (11, 12, 13),
    (14, 15, 16),
    (17, 18, 19),
    (20, 21, 22),
    (23, 24, 25),
    (26, 27, 28),
    (28, 30, 31),
    (32, 33, 34),
    (35, 36, 37),
    (38, 39, 40),
    (41, 42, 43),
    (44, 45, 46),
)


def _rank_score(rating: float, rating_count: int) -> float:
    votes = max(0, rating_count)
    if votes == 0:
        return UNRATED_SCORE
    return ((rating * votes) + (PRIOR_MEAN * PRIOR_WEIGHT)) / (votes + PRIOR_WEIGHT)


def _compare(left: VipTopProfile, right: VipTopProfile) -> int:
    left_has_votes = 1 if left.rating_count > 0 else 0
    right_has_votes = 1 if right.rating_count > 0 else 0
    if left_has_votes != right_has_votes:
        return -1 if left_has_votes > right_has_votes else 1

    left_score = _rank_score(left.rating, left.rating_count)
    right_score = _rank_score(right.rating, right.rating_count)
    if left_score != right_score:
        return -1 if left_score > right_score else 1

    if left.rating != right.rating:
        return -1 if left.rating > right.rating else 1

    if left.rating_count != right.rating_count:
        return -1 if left.rating_count > right.rating_count else 1

    if left.created_at_unix != right.created_at_unix:
        return -1 if left.created_at_unix > right.created_at_unix else 1

    if left.telegram_id != right.telegram_id:
        return -1 if left.telegram_id < right.telegram_id else 1

    return 0


def _age_bracket_key(age: int) -> int | None:
    for bracket in AGE_BRACKETS:
        if age in bracket:
            return bracket[0]
    return None


def _category_key(profile: VipTopProfile) -> tuple[str, int, int] | None:
    age_key = _age_bracket_key(profile.age)
    if age_key is None:
        return None
    city = profile.city.strip().casefold()
    if not city:
        return None
    return city, profile.gender, age_key


def build_gift_recipients(profiles: list[VipTopProfile]) -> list[VipGiftRecipient]:
    grouped: dict[tuple[str, int, int], list[VipTopProfile]] = defaultdict(list)

    for profile in profiles:
        key = _category_key(profile)
        if key is None:
            continue
        grouped[key].append(profile)

    recipients: list[VipGiftRecipient] = []

    for category_profiles in grouped.values():
        if len(category_profiles) < 10:
            continue

        ranked = sorted(category_profiles, key=cmp_to_key(_compare))
        for place, profile in enumerate(ranked[:GIFT_PLACES], start=1):
            recipients.append(
                VipGiftRecipient(
                    telegram_id=profile.telegram_id,
                    place=place,
                    star_price=PLACE_STAR_PRICES[place - 1],
                )
            )

    return recipients
