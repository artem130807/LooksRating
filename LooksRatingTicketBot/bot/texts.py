from bot.html_escape import escape_html


def _admin_user_line(display_name: str | None, telegram_id: int | None) -> str:
    label = escape_html(display_name or "—")
    if telegram_id and telegram_id > 0:
        return f'<a href="tg://user?id={telegram_id}">{label}</a>'
    return label

START_UNAUTH = (
    "Добро пожаловать в бот модерации жалоб.\n\n"
    "Для работы войдите под учётной записью администратора."
)
START_AUTH = (
    "Вы вошли как администратор.\n"
    "Используйте панель внизу или выберите город для просмотра жалоб."
)
ASK_LOGIN = "Введите логин администратора:"
ASK_PASSWORD = "Введите пароль:"
INVALID_CREDENTIALS = "Неверный логин или пароль. Введите логин снова:"
LOGIN_SUCCESS = "Вход выполнен."
MODERATION_HUB_INTRO = (
    "<b>🏙 Города</b>\n"
    "Выберите раздел: жалобы на профили или заявки на вывод звёзд."
)
MODERATION_HUB_HINT = (
    "Выберите раздел кнопкой выше: «📋 Жалобы» или «💫 Заявки на выводы»."
)
WITHDRAWAL_CITY_PROMPT = "Выберите город с заявками на вывод:"
WITHDRAWAL_CITY_HINT = "Выберите город кнопкой в сообщении выше."
WITHDRAWAL_LIST_HINT = "Листайте заявки кнопками под списком или откройте конкретную заявку."
WITHDRAWAL_DETAIL_HINT = "Используйте кнопки под заявкой: выйти, выполнена или отменить."
WITHDRAWAL_QUEUE_EMPTY = (
    "В городе <b>{city}</b> нет ожидающих заявок на вывод.\n"
    "Выберите другой город через «🏙 Города»."
)
NO_WITHDRAWAL_CITIES = "Сейчас нет городов с заявками на вывод."
WITHDRAWAL_NOT_FOUND = "Заявка не найдена."
WITHDRAWAL_CONTEXT_LOST = "Контекст заявки утерян. Откройте «🏙 Города» снова."
WITHDRAWAL_MARKED_CONFIRMED = "✅ Заявка отмечена как выполненная."
WITHDRAWAL_MARKED_CANCELLED = "❌ Заявка отменена."
WITHDRAWAL_STATUS_UPDATE_FAILED = "Не удалось обновить статус заявки."
NO_CITIES = "Сейчас нет городов с активными жалобами."
CITY_SELECTED = (
    "✅ Город: <b>{city}</b>\n"
    "Жалоб в очереди: <b>{count}</b>"
)
QUEUE_EMPTY = (
    "Жалобы по этому городу закончились.\n"
    "Выберите другой город через «🏙 Города»."
)
ACTION_DISMISS_OK = "✅ Жалоба отклонена."
ACTION_DELETE_OK = "✅ Профиль нарушителя удалён."
ACTION_DELETE_ACCOUNT_OK = "✅ Аккаунт нарушителя удалён."
ACTION_SKIP_OK = "➡️ Жалоба пропущена."
LOGOUT_OK = "Вы вышли из аккаунта."
DELETE_PROFILE_CONFIRM = (
    "⚠️ <b>Удалить профиль нарушителя?</b>\n\n"
    "Будут удалены фото профиля и связанные жалобы. "
    "Аккаунт Telegram пользователя останется."
)
DELETE_ACCOUNT_CONFIRM = (
    "⚠️ <b>Удалить аккаунт нарушителя полностью?</b>\n\n"
    "Будут удалены все профили, отзывы и данные пользователя в приложении. "
    "Это действие необратимо."
)
DELETE_CANCELLED = "Удаление отменено."
MODERATION_HINT = (
    "<b>🛡 Панель модерации</b>\n\n"
    "<b>Модерация</b>\n"
    "• <b>🏙 Города</b> — жалобы или заявки на вывод звёзд\n"
    "• <b>📋 Текущая жалоба</b> — показать карточку снова\n\n"
    "<b>Мониторинг</b>\n"
    "• <b>📊 Мониторинг</b> — статус сервисов, алерты, логи\n"
    "• Команда /ops — то же меню\n\n"
    "<b>Аккаунт</b>\n"
    "• <b>❓ Справка</b> — эта подсказка\n"
    "• <b>🚪 Выйти</b> — выход\n\n"
    "<b>Кнопки под фото жалобы</b>\n"
    "• <b>✅ Отклонить</b> — закрыть без санкций\n"
    "• <b>➡️ Пропустить</b> — следующая жалоба\n"
    "• <b>🗑 Удалить профиль</b> / <b>🚫 Удалить аккаунт</b>\n"
    "• <b>🔄 Обновить</b> — перезагрузить карточку"
)
OPS_MENU_INTRO = (
    "<b>📊 Мониторинг</b>\n\n"
    "Проверки запускаются автоматически каждую минуту.\n"
    "Критичные сбои приходят в этот чат push-уведомлением.\n\n"
    "Выберите раздел:"
)
OPS_HELP = (
    "<b>❓ О мониторинге</b>\n\n"
    "<b>Сервисы</b> — API live/ready, cities smoke, Ticket API, TGifts.\n"
    "<b>Telegram</b> — доступность основного бота (getMe).\n"
    "<b>Планировщик</b> — Quartz-задачи по логам (сезон, VIP sparks, лучшая неделя).\n\n"
    "Алерты дедуплицируются: повтор не чаще 30 мин.\n"
    "При восстановлении приходит отдельное уведомление.\n\n"
    "Логи обновляются автоматически каждые 5 с (можно отключить кнопкой)."
)
MONITORING_NO_ALERTS = "✅ <b>Открытых алертов нет</b>\n\nВсе проверки в норме или инциденты уже закрыты."
MONITORING_PICK_SERVICE = "<b>📜 Логи</b>\n\nВыберите сервис:"
MONITORING_LOGS_CLOSED = "Просмотр логов закрыт."
MONITORING_RUN_STARTED = "⏳ Запускаю проверки…"
NOT_IN_MODERATION = (
    "Сначала выберите город с жалобами через «🏙 Города»."
)
LOADING_TICKET = "⏳ Загружаю жалобу…"
UNKNOWN_COMMAND = (
    "Используйте нижнюю панель («🏙 Города», «📋 Текущая жалоба») "
    "или кнопки под фото жалобы."
)

# Backward-compatible alias
DELETE_CONFIRM = DELETE_PROFILE_CONFIRM


def ticket_caption(ticket: dict, remaining: int, city: str | None = None) -> str:
    city_line = f"Город очереди: <b>{escape_html(city)}</b>\n" if city else ""
    description = escape_html(ticket.get("description") or "—")
    reporter = _admin_user_line(
        ticket.get("reporterDisplayName"),
        ticket.get("reporterTelegramId"),
    )
    reporter_city = escape_html(ticket.get("reporterCity"))
    profile_name = _admin_user_line(
        ticket.get("profileDisplayName") or "участник",
        ticket.get("profileTelegramId"),
    )
    profile_city = escape_html(ticket.get("profileCity"))
    profile_gender = escape_html(ticket.get("profileGender"))
    profile_age = escape_html(ticket.get("profileAge"))
    profile_rank = escape_html(ticket.get("profileRank"))
    rating = float(ticket.get("profileRating") or 0)
    rating_count = int(ticket.get("profileRatingCount") or 0)

    return (
        "🚨 <b>Жалоба на модерацию</b>\n\n"
        f"{city_line}"
        f"Текст: {description}\n\n"
        f"От: {reporter} ({reporter_city})\n"
        f"Профиль: {profile_name}\n"
        f"{profile_city} · {profile_gender} · {profile_age} лет\n"
        f"Ранг: {profile_rank} · {rating:.1f} ({rating_count})\n\n"
        f"Осталось в очереди: <b>{remaining}</b>"
    )
