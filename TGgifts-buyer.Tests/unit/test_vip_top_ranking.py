from __future__ import annotations

from helpers.profile_builders import make_category_profiles, make_profile
from services.vip_top_ranking import (
    GIFT_PLACES,
    PLACE_STAR_PRICES,
    build_gift_recipients,
)


class TestBuildGiftRecipients:
    def test_empty_profiles_returns_empty(self) -> None:
        assert build_gift_recipients([]) == []

    def test_category_with_less_than_ten_profiles_is_skipped(self) -> None:
        profiles = make_category_profiles(9)

        recipients = build_gift_recipients(profiles)

        assert recipients == []

    def test_top_five_in_category_receive_star_prices(self) -> None:
        profiles = make_category_profiles(12)

        recipients = build_gift_recipients(profiles)

        assert len(recipients) == GIFT_PLACES
        assert [item.star_price for item in recipients] == list(PLACE_STAR_PRICES)
        assert [item.place for item in recipients] == [1, 2, 3, 4, 5]

    def test_highest_rated_profile_gets_first_place(self) -> None:
        profiles = make_category_profiles(10)
        profiles.append(
            make_profile(
                telegram_id=99_999,
                city="moscow",
                gender=1,
                age=25,
                rating=3.0,
                rating_count=1,
            )
        )

        recipients = build_gift_recipients(profiles)

        assert recipients[0].telegram_id == 10_000
        assert recipients[0].place == 1
        assert recipients[0].star_price == 400

    def test_profiles_without_age_bracket_are_excluded(self) -> None:
        profiles = make_category_profiles(10)
        profiles.append(make_profile(telegram_id=88_888, age=99, rating=10.0, rating_count=100))

        recipients = build_gift_recipients(profiles)

        assert all(recipient.telegram_id != 88_888 for recipient in recipients)
        assert len(recipients) == GIFT_PLACES

    def test_profiles_with_empty_city_are_excluded(self) -> None:
        profiles = make_category_profiles(10)
        profiles.append(make_profile(telegram_id=77_777, city="  ", rating=10.0, rating_count=50))

        recipients = build_gift_recipients(profiles)

        assert all(recipient.telegram_id != 77_777 for recipient in recipients)

    def test_multiple_qualified_categories_produce_five_each(self) -> None:
        moscow = make_category_profiles(10, base_telegram_id=10_000, city="moscow")
        spb = make_category_profiles(10, base_telegram_id=20_000, city="spb")

        recipients = build_gift_recipients(moscow + spb)

        assert len(recipients) == GIFT_PLACES * 2
        moscow_ids = {item.telegram_id for item in recipients if 10_000 <= item.telegram_id < 20_000}
        spb_ids = {item.telegram_id for item in recipients if 20_000 <= item.telegram_id < 30_000}
        assert len(moscow_ids) == GIFT_PLACES
        assert len(spb_ids) == GIFT_PLACES

    def test_unrated_profile_is_not_in_top_five(self) -> None:
        profiles = make_category_profiles(9)
        profiles.append(
            make_profile(
                telegram_id=50_002,
                city="moscow",
                gender=1,
                age=25,
                rating=10.0,
                rating_count=0,
            )
        )

        recipients = build_gift_recipients(profiles)
        recipient_ids = {item.telegram_id for item in recipients}

        assert 50_002 not in recipient_ids
        assert len(recipients) == GIFT_PLACES
        assert recipient_ids.issubset({profile.telegram_id for profile in profiles[:9]})
