package domain

import "errors"

var (
	ErrInvalidTelegramID     = errors.New("telegram id обязателен")
	ErrInvalidAdminID        = errors.New("admin id обязателен")
	ErrInvalidState          = errors.New("недопустимое состояние сессии")
	ErrInvalidRequest        = errors.New("некорректный запрос")
	ErrSessionNotFound       = errors.New("сессия не найдена")
	ErrAdminNotFound         = errors.New("администратор не найден")
	ErrInvalidPassword       = errors.New("неверный пароль")
	ErrAdminNotAuthenticated = errors.New("требуется вход администратора")
	ErrTicketQueueEmpty      = errors.New("жалобы по выбранному городу закончились")
	ErrTicketNotFound        = errors.New("жалоба не найдена")
	ErrUpstreamUnavailable   = errors.New("сервис жалоб временно недоступен")
	ErrUpstreamMisconfigured = errors.New("LooksRating API не обновлён: пересоберите образ api")
	ErrTooManyRequests       = errors.New("слишком много попыток, повторите позже")
	ErrModerationActionFailed = errors.New("действие модерации не выполнено")
)
