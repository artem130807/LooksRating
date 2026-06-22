from bot.age_rules import AGE_BRACKET_INPUT_LABEL

ERROR_MESSAGES: dict[str, str] = {
    "TelegramIdIsRequired": "Некорректный идентификатор Telegram.",
    "InvalidAge": (
        f"Возраст должен быть от {AGE_BRACKET_INPUT_LABEL} лет или «Все возраста»."
    ),
    "InvalidGender": "Выберите пол: мужской, женский или оба.",
    "InvalidCity": "Город не найден в списке. Проверьте написание.",
    "UserAlreadyExists": "Вы уже зарегистрированы.",
    "InvalidTelegramUsername": "Слишком длинное имя пользователя Telegram.",
    "DisplayNameIsRequired": "Укажите имя для отображения другим пользователям.",
    "InvalidDisplayName": "Имя должно быть от 1 до 32 символов.",
    "TelegramUsernameRequiredForDisplay": "У аккаунта нет Telegram username для показа.",
    "RegistrationFailed": "Не удалось завершить регистрацию. Попробуйте ещё раз через /start.",
    "UserNotFound": "Пользователь не найден. Нажмите /start для регистрации.",
    "PhotoAlreadyExists": "У вас уже есть фото в текущем сезоне. Заменить можно в «⚙️ Настройки».",
    "VipPhotoLimitExceeded": "Для VIP максимум 4 фото в текущем сезоне.",
    "PhotoUploadInProgress": "Предыдущее сохранение фото еще выполняется. Подождите пару секунд и попробуйте снова.",
    "VipAlreadyActive": "VIP уже активен. Повторная покупка сейчас не требуется.",
    "PhotoNotFound": "Фото не найдено.",
    "TargetPhotoNotFound": "Выбранное фото для замены не найдено. Обновите экран и попробуйте снова.",
    "PhotoIdsRequired": "Добавьте хотя бы одно фото для замены.",
    "TooManyPhotosForNonVip": "Без VIP можно хранить только одну фотографию.",
    "TooManyPhotosForVip": "Для VIP можно сохранить максимум 4 фотографии.",
    "CurrentSeasonNotFound": "Текущий сезон недоступен. Попробуйте позже.",
    "UserProfileIncomplete": "Заполните настройки ленты: город, возраст и пол.",
    "RecomendationSettingsIncomplete": "Настройте ленту: город, возраст и пол. Нажмите «⭐ Оценить».",
    "RecomendationSettingsNotFound": "Сначала настройте ленту через «⭐ Оценить» или «⚙️ Настройки → Моя лента».",
    "InvalidNominationCity": "Укажите корректный город для номинации.",
    "InvalidNominationAge": f"Возраст номинации должен быть от {AGE_BRACKET_INPUT_LABEL}.",
    "InvalidNominationGender": "Укажите пол для номинации.",
    "ReviewerNotFound": "Пользователь не найден.",
    "PhotoProfileNotFound": "Профиль с фото не найден.",
    "SelfReviewIsNotAllowed": "Нельзя оценивать своё фото.",
    "NoPhotosAvailable": "Сейчас нет новых фото для оценки в вашей ленте.",
    "ReviewAlreadyExists": "Вы уже оценили это фото.",
    "InvalidRatingValue": "Оценка должна быть от 1 до 10.",
    "ReporterNotFound": "Пользователь не найден.",
    "SelfComplaintIsNotAllowed": "Нельзя пожаловаться на своё фото.",
    "TicketAlreadyExists": "Жалоба на это фото уже отправлена.",
    "DescriptionIsRequired": "Введите текст жалобы.",
    "DescriptionTooLong": "Слишком длинное описание жалобы.",
    "PhotoProfileIdIsRequired": "Не указан профиль для жалобы.",
    "Фотография не найдена": "Сейчас нет подходящих фото для оценки. Попробуйте позже.",
    "Пользователь не найдён": "Пользователь не найден.",
    "SeasonNotFound": "Сезон не найден.",
    "TooManyRequests": "Вы превысили лимит сообщений. Подождите немного и попробуйте снова.",
}

SERVER_UNAVAILABLE_MESSAGE = (
    "Бот временно не отвечает. Подождите немного и попробуйте снова."
)


def _is_http_status_message(value: str) -> bool:
    normalized = value.strip()
    if not normalized.upper().startswith("HTTP "):
        return False
    status_part = normalized[5:].strip()
    return status_part.isdigit()


def translate_error(
    code: str | None,
    fallback: str | None = None,
    *,
    status: int | None = None,
) -> str:
    if status is not None and status >= 500:
        return SERVER_UNAVAILABLE_MESSAGE
    if code and code in ERROR_MESSAGES:
        return ERROR_MESSAGES[code]
    if fallback:
        if _is_http_status_message(fallback):
            status_from_message = int(fallback.strip().split()[-1])
            if status_from_message >= 500:
                return SERVER_UNAVAILABLE_MESSAGE
        return fallback
    return "Произошла ошибка. Попробуйте ещё раз."
