ERROR_MESSAGES: dict[str, str] = {
    "TelegramIdIsRequired": "Некорректный идентификатор Telegram.",
    "InvalidAge": "Возраст должен быть от 14 до 100 лет.",
    "InvalidGender": "Выберите пол: мужской, женский или оба.",
    "InvalidCity": "Город не найден в списке. Проверьте написание.",
    "UserAlreadyExists": "Вы уже зарегистрированы.",
    "InvalidTelegramUsername": "Слишком длинное имя пользователя Telegram.",
    "DisplayNameIsRequired": "Укажите имя для отображения другим пользователям.",
    "InvalidDisplayName": "Имя должно быть от 1 до 32 символов.",
    "TelegramUsernameRequiredForDisplay": "У аккаунта нет Telegram username для показа.",
    "UserNotFound": "Пользователь не найден. Нажмите /start для регистрации.",
    "PhotoAlreadyExists": "У вас уже есть фото в текущем сезоне. Заменить можно в «⚙️ Настройки».",
    "PhotoNotFound": "Фото не найдено.",
    "CurrentSeasonNotFound": "Текущий сезон недоступен. Попробуйте позже.",
    "UserProfileIncomplete": "Заполните настройки ленты: город, возраст и пол.",
    "RecomendationSettingsIncomplete": "Настройте ленту: город, возраст и пол. Нажмите «⭐ Оценить».",
    "RecomendationSettingsNotFound": "Сначала настройте ленту через «⭐ Оценить» или «⚙️ Настройки → Моя лента».",
    "InvalidNominationCity": "Укажите корректный город для номинации.",
    "InvalidNominationAge": "Возраст номинации должен быть от 14 до 100.",
    "InvalidNominationGender": "Укажите пол для номинации.",
    "ReviewerNotFound": "Пользователь не найден.",
    "PhotoUserNotFound": "Фото для жалобы не найдено.",
    "SelfReviewIsNotAllowed": "Нельзя оценивать своё фото.",
    "NoPhotosAvailable": "Сейчас нет новых фото для оценки в вашей ленте.",
    "ReviewAlreadyExists": "Вы уже оценили это фото.",
    "InvalidRatingValue": "Оценка должна быть от 1 до 10.",
    "ReporterNotFound": "Пользователь не найден.",
    "SelfComplaintIsNotAllowed": "Нельзя пожаловаться на своё фото.",
    "TicketAlreadyExists": "Жалоба на это фото уже отправлена.",
    "DescriptionIsRequired": "Введите текст жалобы.",
    "DescriptionTooLong": "Слишком длинное описание жалобы.",
    "PhotoUserIdIsRequired": "Не указано фото для жалобы.",
    "Фотография не найдена": "Сейчас нет подходящих фото для оценки. Попробуйте позже.",
    "Пользователь не найдён": "Пользователь не найден.",
    "SeasonNotFound": "Сезон не найден.",
}


def translate_error(code: str | None, fallback: str | None = None) -> str:
    if code and code in ERROR_MESSAGES:
        return ERROR_MESSAGES[code]
    if fallback:
        return fallback
    return "Произошла ошибка. Попробуйте ещё раз."
