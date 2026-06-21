from aiogram.types import InlineKeyboardButton, InlineKeyboardMarkup, KeyboardButton, ReplyKeyboardMarkup

# Inline callbacks — auth / moderation
CALLBACK_LOGIN = "auth:login"
CALLBACK_LOGOUT = "auth:logout"
CALLBACK_CHANGE_CITY = "mod:change_city"
CALLBACK_DISMISS = "mod:dismiss"
CALLBACK_DELETE = "mod:delete"
CALLBACK_DELETE_ACCOUNT = "mod:delete_account"
CALLBACK_DELETE_CONFIRM = "mod:delete_confirm"
CALLBACK_DELETE_ACCOUNT_CONFIRM = "mod:delete_account_confirm"
CALLBACK_DELETE_CANCEL = "mod:delete_cancel"
CALLBACK_SKIP = "mod:skip"
CALLBACK_CURRENT = "mod:current"
CALLBACK_HELP = "mod:help"
CALLBACK_PREFIX_CITY = "city:"
CALLBACK_MOD_HUB_COMPLAINTS = "mod:hub:complaints"
CALLBACK_MOD_HUB_WITHDRAWALS = "mod:hub:withdrawals"
CALLBACK_PREFIX_WITHDRAWAL_CITY = "wcity:"
CALLBACK_PREFIX_WITHDRAWAL_PAGE = "wos:page:"
CALLBACK_PREFIX_WITHDRAWAL_OPEN = "wos:open:"
CALLBACK_WITHDRAWAL_BACK = "wos:back"
CALLBACK_PREFIX_WITHDRAWAL_EXIT = "wos:exit:"
CALLBACK_PREFIX_WITHDRAWAL_DONE = "wos:done:"
CALLBACK_PREFIX_WITHDRAWAL_CANCEL = "wos:cancel:"

# Reply keyboard
BTN_CITIES = "🏙 Города"
BTN_CURRENT = "📋 Текущая жалоба"
BTN_OPS = "📊 Мониторинг"
BTN_HELP = "❓ Справка"
BTN_LOGOUT = "🚪 Выйти"

# Legacy aliases (старые кнопки → хаб мониторинга)
BTN_STATUS = BTN_OPS
BTN_RUN_CHECKS = BTN_OPS
BTN_LOGS = BTN_OPS
BTN_ALERTS = BTN_OPS

ADMIN_PANEL_BUTTONS = frozenset(
    {
        BTN_CITIES,
        BTN_CURRENT,
        BTN_OPS,
        BTN_HELP,
        BTN_LOGOUT,
    }
)

# Кнопки модерации (без мониторинга — его маршрутизирует handlers/admin_panel.py)
MODERATION_PANEL_BUTTONS = frozenset(
    {
        BTN_CITIES,
        BTN_CURRENT,
        BTN_HELP,
        BTN_LOGOUT,
    }
)

# Inline callbacks — мониторинг
CALLBACK_OPS_HOME = "ops:home"
CALLBACK_OPS_STATUS = "ops:status"
CALLBACK_OPS_RUN = "ops:run"
CALLBACK_OPS_ALERTS = "ops:alerts"
CALLBACK_OPS_LOGS = "ops:logs"
CALLBACK_OPS_HELP = "ops:help"
CALLBACK_OPS_LOG_SERVICE = "ops:log:"
CALLBACK_OPS_LOG_REFRESH = "ops:log_refresh"
CALLBACK_OPS_LOG_AUTO = "ops:log_auto"
CALLBACK_OPS_LOG_BACK = "ops:log_back"
CALLBACK_OPS_LOG_HOME = "ops:log_home"

LOG_SERVICES: dict[str, str] = {
    "api": "🌐 API",
    "bot": "🤖 Main bot",
    "ticket-api": "🎫 Ticket API",
    "ticket-bot": "🎫 Ticket bot",
    "tgifts-buyer": "🎁 TGifts",
}


def start_unauthenticated() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[[InlineKeyboardButton(text="🔐 Войти", callback_data=CALLBACK_LOGIN)]]
    )


def admin_reply_keyboard() -> ReplyKeyboardMarkup:
    """Панель: модерация + мониторинг + аккаунт."""
    return ReplyKeyboardMarkup(
        keyboard=[
            [KeyboardButton(text=BTN_CITIES), KeyboardButton(text=BTN_CURRENT)],
            [KeyboardButton(text=BTN_OPS)],
            [KeyboardButton(text=BTN_HELP), KeyboardButton(text=BTN_LOGOUT)],
        ],
        resize_keyboard=True,
        is_persistent=True,
    )


def ops_hub(alerts_count: int = 0) -> InlineKeyboardMarkup:
    alerts_label = "🔔 Алерты"
    if alerts_count > 0:
        alerts_label = f"🔔 Алерты ({alerts_count})"
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="📊 Статус", callback_data=CALLBACK_OPS_STATUS),
                InlineKeyboardButton(text="🔄 Проверить", callback_data=CALLBACK_OPS_RUN),
            ],
            [InlineKeyboardButton(text=alerts_label, callback_data=CALLBACK_OPS_ALERTS)],
            [InlineKeyboardButton(text="📜 Логи сервисов", callback_data=CALLBACK_OPS_LOGS)],
            [InlineKeyboardButton(text="❓ О мониторинге", callback_data=CALLBACK_OPS_HELP)],
        ]
    )


def ops_status_actions() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="🔄 Проверить снова", callback_data=CALLBACK_OPS_RUN),
                InlineKeyboardButton(text="🔔 Алерты", callback_data=CALLBACK_OPS_ALERTS),
            ],
            [InlineKeyboardButton(text="◀️ Мониторинг", callback_data=CALLBACK_OPS_HOME)],
        ]
    )


def ops_alerts_actions() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="📊 Статус", callback_data=CALLBACK_OPS_STATUS),
                InlineKeyboardButton(text="🔄 Проверить", callback_data=CALLBACK_OPS_RUN),
            ],
            [InlineKeyboardButton(text="◀️ Мониторинг", callback_data=CALLBACK_OPS_HOME)],
        ]
    )


def monitoring_log_services() -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    for service_id, label in LOG_SERVICES.items():
        rows.append(
            [
                InlineKeyboardButton(
                    text=label,
                    callback_data=f"{CALLBACK_OPS_LOG_SERVICE}{service_id}",
                )
            ]
        )
    rows.append([InlineKeyboardButton(text="◀️ Мониторинг", callback_data=CALLBACK_OPS_HOME)])
    return InlineKeyboardMarkup(inline_keyboard=rows)


def monitoring_log_actions(auto_refresh: bool = True) -> InlineKeyboardMarkup:
    auto_label = "⏸ Авто 5с" if auto_refresh else "▶️ Авто 5с"
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="🔄 Обновить", callback_data=CALLBACK_OPS_LOG_REFRESH),
                InlineKeyboardButton(text=auto_label, callback_data=CALLBACK_OPS_LOG_AUTO),
            ],
            [
                InlineKeyboardButton(text="◀️ К логам", callback_data=CALLBACK_OPS_LOG_BACK),
                InlineKeyboardButton(text="📊 Мониторинг", callback_data=CALLBACK_OPS_LOG_HOME),
            ],
        ]
    )


def moderation_hub() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [InlineKeyboardButton(text="📋 Жалобы", callback_data=CALLBACK_MOD_HUB_COMPLAINTS)],
            [
                InlineKeyboardButton(
                    text="💫 Заявки на выводы",
                    callback_data=CALLBACK_MOD_HUB_WITHDRAWALS,
                )
            ],
        ]
    )


def withdrawal_city_selection(cities: list[str]) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    for index, city in enumerate(cities):
        rows.append(
            [
                InlineKeyboardButton(
                    text=city,
                    callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_CITY}{index}",
                )
            ]
        )
    return InlineKeyboardMarkup(inline_keyboard=rows)


def withdrawal_list_keyboard(
    items: list,
    *,
    page: int,
    has_next_page: bool,
) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    from bot.withdrawal_views import withdrawal_list_button_label

    for index, item in enumerate(items):
        rows.append(
            [
                InlineKeyboardButton(
                    text=withdrawal_list_button_label(item, index_on_page=index),
                    callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_OPEN}{item.id}",
                )
            ]
        )

    nav_row: list[InlineKeyboardButton] = []
    if page > 1:
        nav_row.append(
            InlineKeyboardButton(
                text="◀️ Назад",
                callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_PAGE}{page - 1}",
            )
        )
    if has_next_page:
        nav_row.append(
            InlineKeyboardButton(
                text="Вперёд ▶️",
                callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_PAGE}{page + 1}",
            )
        )
    if nav_row:
        rows.append(nav_row)

    return InlineKeyboardMarkup(inline_keyboard=rows)


def withdrawal_detail_actions(request_id: str, *, allow_status_change: bool = True) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = [
        [
            InlineKeyboardButton(
                text="↩️ Выйти из заявки",
                callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_EXIT}{request_id}",
            )
        ],
    ]
    if allow_status_change:
        rows.append(
            [
                InlineKeyboardButton(
                    text="✅ Выполнена",
                    callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_DONE}{request_id}",
                ),
                InlineKeyboardButton(
                    text="❌ Отменить",
                    callback_data=f"{CALLBACK_PREFIX_WITHDRAWAL_CANCEL}{request_id}",
                ),
            ]
        )
    return InlineKeyboardMarkup(inline_keyboard=rows)


def city_selection(cities: list[str]) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    for index, city in enumerate(cities):
        rows.append(
            [
                InlineKeyboardButton(
                    text=city,
                    callback_data=f"{CALLBACK_PREFIX_CITY}{index}",
                )
            ]
        )
    return InlineKeyboardMarkup(inline_keyboard=rows)


def moderation_actions() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="✅ Отклонить", callback_data=CALLBACK_DISMISS),
                InlineKeyboardButton(text="➡️ Пропустить", callback_data=CALLBACK_SKIP),
            ],
            [InlineKeyboardButton(text="🗑 Удалить профиль", callback_data=CALLBACK_DELETE)],
            [InlineKeyboardButton(text="🚫 Удалить аккаунт", callback_data=CALLBACK_DELETE_ACCOUNT)],
            [InlineKeyboardButton(text="🔄 Обновить", callback_data=CALLBACK_CURRENT)],
        ]
    )


def delete_profile_confirmation() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="✅ Да, удалить профиль", callback_data=CALLBACK_DELETE_CONFIRM),
                InlineKeyboardButton(text="❌ Отмена", callback_data=CALLBACK_DELETE_CANCEL),
            ]
        ]
    )


def delete_account_confirmation() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(
                    text="✅ Да, удалить аккаунт",
                    callback_data=CALLBACK_DELETE_ACCOUNT_CONFIRM,
                ),
                InlineKeyboardButton(text="❌ Отмена", callback_data=CALLBACK_DELETE_CANCEL),
            ]
        ]
    )


def authenticated_menu() -> InlineKeyboardMarkup:
    return InlineKeyboardMarkup(
        inline_keyboard=[
            [
                InlineKeyboardButton(text="🏙 Выбрать город", callback_data=CALLBACK_CHANGE_CITY),
                InlineKeyboardButton(text="📋 Текущая жалоба", callback_data=CALLBACK_CURRENT),
            ],
            [InlineKeyboardButton(text="🚪 Выйти", callback_data=CALLBACK_LOGOUT)],
        ]
    )


def delete_confirmation() -> InlineKeyboardMarkup:
    return delete_profile_confirmation()
