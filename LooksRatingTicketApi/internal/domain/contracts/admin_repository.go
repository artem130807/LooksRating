package contracts

import (
	"context"

	"looksrating/ticketapi/internal/domain"
)

type AdminRepository interface {
	GetByID(ctx context.Context, id uint) (*domain.Admin, error)
	GetByTelegramID(ctx context.Context, telegramID int64) (*domain.Admin, error)
	GetByFirstName(ctx context.Context, firstName string) (*domain.Admin, error)
	ListActiveWithTelegram(ctx context.Context) ([]domain.Admin, error)
	Create(ctx context.Context, admin *domain.Admin) error
	Update(ctx context.Context, admin *domain.Admin) error
}
