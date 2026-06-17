from bot import texts


def photo_settings_intro(*, has_vip: bool, recreate: bool, replace_all: bool) -> str:
    """Краткое пояснение перед сменой/добавлением фото из «Настройки»."""
    if has_vip:
        if replace_all:
            return texts.PHOTO_VIP_INTRO_REPLACE_ALL
        if recreate:
            return texts.PHOTO_VIP_INTRO_REPLACE
        return texts.PHOTO_VIP_INTRO_ADD

    if replace_all or recreate:
        return texts.PHOTO_NON_VIP_INTRO_REPLACE
    return texts.PHOTO_NON_VIP_INTRO_ADD
