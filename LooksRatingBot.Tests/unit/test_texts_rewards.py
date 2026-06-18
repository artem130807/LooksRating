from bot import texts


def test_season_top_rewards_program_lists_all_places() -> None:
    assert "800" in texts.SEASON_TOP_REWARDS_PROGRAM
    assert "200" in texts.SEASON_TOP_REWARDS_PROGRAM
    assert "10" in texts.SEASON_TOP_REWARDS_PROGRAM
    assert "каждый" in texts.SEASON_TOP_REWARDS_PROGRAM
    assert texts.SEASON_TOP_REWARDS_LADDER in texts.SEASON_TOP_REWARDS_PROGRAM


def test_bot_info_includes_season_rewards() -> None:
    assert texts.SEASON_TOP_REWARDS_PROGRAM in texts.BOT_INFO
    assert texts.VIP_REWARDS_PROGRAM in texts.BOT_INFO


def test_shop_menu_is_vip_only() -> None:
    assert texts.SEASON_TOP_REWARDS_PROGRAM not in texts.VIP_SHOP_MENU
    assert texts.VIP_REWARDS_PROGRAM in texts.VIP_SHOP_MENU
    assert texts.VIP_FEATURES in texts.VIP_SHOP_MENU
    assert texts.VIP_SHOP_MENU == texts.SHOP_MENU


def test_privileges_hub_mentions_vip_and_referral() -> None:
    assert "VIP" in texts.PRIVILEGES_HUB
    assert "реферальная программа" in texts.PRIVILEGES_HUB.lower()


def test_referral_program_describes_reward() -> None:
    assert "15" in texts.REFERRAL_PROGRAM_INTRO
    assert "{invited}/{max_invited}" in texts.REFERRAL_PROGRAM_INVITE_STATS
    assert "приглашённых пользователей" in texts.REFERRAL_PROGRAM_INVITE_STATS


def test_shop_vip_paid_excludes_season_rewards() -> None:
    assert texts.SEASON_TOP_REWARDS_PROGRAM not in texts.SHOP_VIP_PAID


def test_tops_menu_mentions_season_sparks() -> None:
    assert "сезон" in texts.TOPS_MENU.lower()
    assert "искр" in texts.TOPS_MENU.lower()


def test_vip_rewards_distinct_from_season() -> None:
    assert "отдельно от сезонного топа" in texts.VIP_REWARDS_PROGRAM
