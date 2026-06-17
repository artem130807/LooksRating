package contracts

import (
	"context"

	"looksrating/ticketapi/internal/domain"
)

type UserSessionRepository interface {
	GetByTelegramID(ctx context.Context, telegramID int64) (*domain.UserSession, error)
	GetByTelegramIDForUpdate(ctx context.Context, telegramID int64) (*domain.UserSession, error)
	ExistsByTelegramID(ctx context.Context, telegramID int64) (bool, error)
	Create(ctx context.Context, session *domain.UserSession) error
	Update(ctx context.Context, session *domain.UserSession) error
	// Mutate выполняет fn в транзакции с SELECT FOR UPDATE.
	Mutate(ctx context.Context, telegramID int64, fn func(*domain.UserSession) error) (*domain.UserSession, error)
	// BindAdminLogin атомарно привязывает admin к telegram и обновляет сессию.
	BindAdminLogin(ctx context.Context, telegramID int64, adminID uint) (*domain.UserSession, error)
}
