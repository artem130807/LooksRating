from aiogram.types import (
    InlineKeyboardButton,
    InlineKeyboardMarkup,
    KeyboardButton,
    ReplyKeyboardMarkup,
    ReplyKeyboardRemove,
)

from bot import callbacks

MENU_RATE = "⭐ Оценить"
MENU_ABOUT = "ℹ️ О боте"
MENU_TOP = "🏆 Топы"
MENU_PROFILE = "👤 Профиль"
MENU_SETTINGS = "⚙️ Настройки"
MENU_PRIVILEGES = "✨ Привилегии"
MENU_PHOTO_ADD = "📷 Добавить фото"
MENU_PHOTO_REPLACE = "📷 Заменить фото"
MENU_PHOTO_REPLACE_ALL = "🖼 Сменить все фото"
MENU_CANCEL = "❌ Отмена"
MENU_BACK = "◀️ Назад"

VIP_MAX_PHOTOS = 4

SETTINGS_PHOTO_BUTTONS = {MENU_PHOTO_ADD, MENU_PHOTO_REPLACE, MENU_PHOTO_REPLACE_ALL}

BTN_SETTINGS_FEED = "🏙 Моя лента"
BTN_DELETE_ACCOUNT = "🗑 Удалить аккаунт"
BTN_DELETE_CONFIRM = "✅ Да, удалить аккаунт"

BTN_YES = "✅ Да, добавить"
BTN_NO = "⏭ Позже"
BTN_DISPLAY_USE_TELEGRAM = "✅ Да, показывать"
BTN_DISPLAY_CUSTOM = "✏️ Нет, указать имя"
BTN_PROFILE_NOMINATION = "📋 Как в ленте"
BTN_CUSTOM_NOMINATION = "✏️ Своя номинация"
BTN_COMPLAIN = "🚩 Жалоба"

GENDER_MALE = "👨 Мужской"
GENDER_FEMALE = "👩 Женский"
GENDER_BOTH = "👥 Оба"

BTN_EDIT_CITY = "🏙 Город"
BTN_EDIT_AGE = "🎂 Возраст"
BTN_EDIT_GENDER = "👤 Пол"
BTN_AGE_ALL = "🌐 Все возраста"
BTN_RATING_EXIT = "⏹ Выйти из оценки"
BTN_SEASON_TOP = "🏆 Топ сезона"
BTN_SEASON_MY_PHOTO = "📸 Моё фото"
BTN_SHOP_VIP = "⭐ Купить VIP"
BTN_SHOP_GIFTS = "🎁 Подарок за искры"
BTN_PRIVILEGES_VIP = "⭐ VIP-статус"
BTN_PRIVILEGES_REFERRAL = "🔗 Реферальная программа"
BTN_PRIVILEGES_BACK = "◀️ К привилегиям"
BTN_MAIN_MENU = "📱 В меню"
BTN_REFERRAL_SHARE = "📤 Поделиться"


def main_menu() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=MENU_RATE)],
            [KeyboardButton(text=MENU_ABOUT), KeyboardButton(text=MENU_TOP)],
            [KeyboardButton(text=MENU_PROFILE), KeyboardButton(text=MENU_SETTINGS)],
            [KeyboardButton(text=MENU_PRIVILEGES)],
        ],
        resize_keyboard=True,
        input_field_placeholder="Выберите действие в меню",
    )


def settings_keyboard(
    *,
    has_photo: bool = False,
    has_vip: bool = False,
    photo_count: int = 0,
    can_add_photo: bool = False,
) -> ReplyKeyboardMarkup:
    photo_row: list[KeyboardButton] = []
    if has_vip:
        if can_add_photo:
            photo_row.append(KeyboardButton(text=MENU_PHOTO_ADD))
        if has_photo:
            photo_row.append(KeyboardButton(text=MENU_PHOTO_REPLACE))
            photo_row.append(KeyboardButton(text=MENU_PHOTO_REPLACE_ALL))
    else:
        photo_row.append(KeyboardButton(text=MENU_PHOTO_REPLACE if has_photo else MENU_PHOTO_ADD))

    keyboard = [
        [KeyboardButton(text=BTN_SETTINGS_FEED)],
        photo_row if photo_row else [KeyboardButton(text=MENU_PHOTO_ADD)],
        [KeyboardButton(text=BTN_DELETE_ACCOUNT)],
        [KeyboardButton(text=MENU_BACK)],
    ]

    return ReplyKeyboardMarkup(
        keyboard=keyboard,
        resize_keyboard=True,
        input_field_placeholder="Выберите раздел настроек",
    )


def delete_account_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_DELETE_CONFIRM)],
            [KeyboardButton(text=MENU_BACK)],
        ],
        resize_keyboard=True,
    )


def cancel_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[[KeyboardButton(text=MENU_CANCEL)]],
        resize_keyboard=True,
        input_field_placeholder="Или нажмите «Отмена»",
    )


def multi_photo_upload_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text="✅ Сохранить набор")],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
        input_field_placeholder="Отправьте фото и нажмите «Сохранить набор»",
    )


def age_input_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_AGE_ALL)],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
        input_field_placeholder="Введите возраст или выберите «Все возраста»",
    )


def display_name_choice_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_DISPLAY_USE_TELEGRAM), KeyboardButton(text=BTN_DISPLAY_CUSTOM)],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
    )


def yes_no_photo_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_YES), KeyboardButton(text=BTN_NO)],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
    )


def nomination_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_PROFILE_NOMINATION)],
            [KeyboardButton(text=BTN_CUSTOM_NOMINATION)],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
    )


def feed_gender_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=GENDER_MALE), KeyboardButton(text=GENDER_FEMALE)],
            [KeyboardButton(text=GENDER_BOTH)],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
    )


def gender_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=GENDER_MALE), KeyboardButton(text=GENDER_FEMALE)],
            [KeyboardButton(text=MENU_CANCEL)],
        ],
        resize_keyboard=True,
    )


def profile_edit_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_EDIT_CITY), KeyboardButton(text=BTN_EDIT_AGE)],
            [KeyboardButton(text=BTN_EDIT_GENDER)],
            [KeyboardButton(text=MENU_BACK)],
        ],
        resize_keyboard=True,
    )


def rating_flow_keyboard() -> ReplyKeyboardMarkup:
    return ReplyKeyboardMarkup(
        keyboard=[[KeyboardButton(text=BTN_RATING_EXIT)]],
        resize_keyboard=True,
        input_field_placeholder="Оцените фото кнопками 1–10",
    )


def chapters_list_keyboard(
    chapters: list[dict],
    current_chapter_id: str | None,
) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    total = len(chapters)
    for idx, chapter in enumerate(chapters, start=1):
        chapter_id = str(chapter.get("id"))
        seasons = chapter.get("seasons")
        seasons_count = len(seasons) if isinstance(seasons, list) else chapter.get("seasonsCount")
        label = f"📚 Глава {total - idx + 1}"
        if isinstance(seasons_count, int):
            label += f" · сезонов: {seasons_count}"
        if current_chapter_id and chapter_id == current_chapter_id:
            label += " · сейчас"
        rows.append(
            [InlineKeyboardButton(text=label, callback_data=f"chapter:open:{chapter_id}")]
        )
    rows.append([InlineKeyboardButton(text="📱 В меню", callback_data="season:menu")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def seasons_list_keyboard(
    seasons: list[dict],
    current_season_id: str | None,
) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    for season in sorted(seasons, key=lambda item: item.get("number", 0), reverse=True):
        season_id = str(season["id"])
        label = f"№{season.get('number', '?')} {season.get('name', 'Сезон')}"
        if current_season_id and season_id == current_season_id:
            label += " · сейчас"
        if season.get("isClosed"):
            label += " 🔒"
        rows.append(
            [InlineKeyboardButton(text=label, callback_data=f"season:open:{season_id}")]
        )
    rows.append([InlineKeyboardButton(text="📚 К главам", callback_data="chapter:list")])
    rows.append([InlineKeyboardButton(text="📱 В меню", callback_data="season:menu")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def season_actions_keyboard(season_id: str) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(
                    text=BTN_SEASON_TOP,
                    callback_data=f"top:pick:{season_id}",
                ),
                InlineKeyboardButton(
                    text=BTN_SEASON_MY_PHOTO,
                    callback_data=f"top:my:{season_id}",
                ),
            ],
            [
                InlineKeyboardButton(text="📅 К сезонам главы", callback_data="chapter:back"),
                InlineKeyboardButton(text="📱 В меню", callback_data="season:menu"),
            ],
        ]
    )


def tops_menu_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="🔥 Топ-10 недели", callback_data="top:weekly:pick")],
            [InlineKeyboardButton(text="💎 Топ-10 VIP", callback_data="top:vip:pick")],
            [InlineKeyboardButton(text="📅 Топы по сезонам", callback_data="chapter:list")],
            [InlineKeyboardButton(text="📱 В меню", callback_data="top:menu")],
        ]
    )


def top_notification_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="🔥 Открыть топ-10 недели", callback_data="top:weekly:pick")],
            [InlineKeyboardButton(text="📅 Топы по сезонам", callback_data="chapter:list")],
        ]
    )


def review_milestone_notification_keyboard(notification_id: str) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(
                    text="👥 Посмотреть профили",
                    callback_data=f"review_milestone:view:{notification_id}",
                )
            ],
        ]
    )


def weekly_scope_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="⚡ Эта неделя", callback_data="top:weekly:current")],
            [InlineKeyboardButton(text="📦 Прошлая неделя", callback_data="top:weekly:previous")],
            [InlineKeyboardButton(text="📱 В меню", callback_data="top:menu")],
        ]
    )


def top_gender_pick_keyboard(scope: str, season_id: str | None = None) -> InlineKeyboardMarkup:
    if scope == "season":
        male_callback = f"top:open:season:{season_id}:1:1"
        female_callback = f"top:open:season:{season_id}:1:2"
        back_callback = "chapter:back"
    elif scope == "weekly_previous":
        male_callback = "top:open:weekly:previous:1"
        female_callback = "top:open:weekly:previous:2"
        back_callback = "top:weekly:pick"
    elif scope == "vip":
        male_callback = "top:open:vip:1"
        female_callback = "top:open:vip:2"
        back_callback = "top:tops"
    else:
        male_callback = "top:open:weekly:1"
        female_callback = "top:open:weekly:2"
        back_callback = "top:weekly:pick"

    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="👨 Парни", callback_data=male_callback),
                InlineKeyboardButton(text="👩 Девушки", callback_data=female_callback),
            ],
            [InlineKeyboardButton(text="◀️ Назад", callback_data=back_callback)],
        ]
    )


def privileges_hub_keyboard(*, has_vip: bool = False) -> InlineKeyboardMarkup:
    vip_label = BTN_PRIVILEGES_VIP
    if has_vip:
        vip_label = f"{BTN_PRIVILEGES_VIP} · активен"
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text=vip_label, callback_data=callbacks.PRIVILEGES_VIP)],
            [InlineKeyboardButton(text=BTN_PRIVILEGES_REFERRAL, callback_data=callbacks.PRIVILEGES_REFERRAL)],
            [InlineKeyboardButton(text=BTN_MAIN_MENU, callback_data=callbacks.SHOP_MAIN_MENU)],
        ]
    )


def vip_shop_keyboard(*, has_vip: bool = False) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = [
        [InlineKeyboardButton(text=BTN_SHOP_VIP, callback_data=callbacks.SHOP_VIP_BUY)],
    ]
    if has_vip:
        rows.append(
            [InlineKeyboardButton(text=BTN_SHOP_GIFTS, callback_data=callbacks.SHOP_GIFTS)]
        )
    rows.append(
        [InlineKeyboardButton(text=BTN_PRIVILEGES_BACK, callback_data=callbacks.PRIVILEGES_HUB)]
    )
    rows.append(
        [InlineKeyboardButton(text=BTN_MAIN_MENU, callback_data=callbacks.SHOP_MAIN_MENU)]
    )
    return InlineKeyboardMarkup(inline_keyboard=rows)


def referral_program_keyboard(*, share_url: str | None) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    if share_url:
        rows.append(
            [InlineKeyboardButton(text=BTN_REFERRAL_SHARE, url=share_url)]
        )
    rows.append(
        [InlineKeyboardButton(text=BTN_PRIVILEGES_BACK, callback_data=callbacks.PRIVILEGES_HUB)]
    )
    rows.append(
        [InlineKeyboardButton(text=BTN_MAIN_MENU, callback_data=callbacks.SHOP_MAIN_MENU)]
    )
    return InlineKeyboardMarkup(inline_keyboard=rows)


def shop_keyboard(*, has_vip: bool = False) -> InlineKeyboardMarkup:
    """Backward-compatible alias for the VIP shop inline keyboard."""
    return vip_shop_keyboard(has_vip=has_vip)


def shop_gifts_keyboard() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="100★ · 1 000 искр", callback_data="shop:gift:select:100")],
            [InlineKeyboardButton(text="200★ · 2 000 искр", callback_data="shop:gift:select:200")],
            [InlineKeyboardButton(text="300★ · 3 000 искр", callback_data="shop:gift:select:300")],
            [InlineKeyboardButton(text="400★ · 4 000 искр", callback_data="shop:gift:select:400")],
            [InlineKeyboardButton(text="◀️ Назад", callback_data=callbacks.SHOP_BACK)],
        ]
    )


def shop_gift_confirm_keyboard(stars_count: int) -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(
                    text="✅ Подтвердить",
                    callback_data=f"shop:gift:confirm:{stars_count}",
                )
            ],
            [InlineKeyboardButton(text="❌ Отмена", callback_data=callbacks.SHOP_GIFTS)],
        ]
    )


def replace_photo_picker_keyboard(photos: list[dict]) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    for idx, photo in enumerate(photos, start=1):
        photo_id = str(photo.get("id", "")).strip()
        if not photo_id:
            continue
        rows.append(
            [
                InlineKeyboardButton(
                    text=f"Фото {idx}",
                    callback_data=f"replace:pick:{photo_id}",
                )
            ]
        )
    rows.append([InlineKeyboardButton(text="❌ Отмена", callback_data="replace:cancel")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def rating_keyboard(photo_id: str) -> InlineKeyboardMarkup:
    low = [
        InlineKeyboardButton(text=str(i), callback_data=f"rate:{photo_id}:{i}")
        for i in range(1, 6)
    ]
    high = [
        InlineKeyboardButton(text=str(i), callback_data=f"rate:{photo_id}:{i}")
        for i in range(6, 11)
    ]
    return InlineKeyboardMarkup(
        inline_keyboard=[
            low,
            high,
            [
                InlineKeyboardButton(text=BTN_COMPLAIN, callback_data=f"complain:{photo_id}"),
                InlineKeyboardButton(text=BTN_RATING_EXIT, callback_data="rate:exit"),
            ],
        ]
    )


def remove_keyboard() -> ReplyKeyboardRemove:
    return ReplyKeyboardRemove()
