package persistence

import (
	"context"
	"errors"
	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"

	"gorm.io/gorm"
)

type AdminRepository struct {
	db *gorm.DB
}

func NewAdminRepository(db *gorm.DB) contracts.AdminRepository {
	return &AdminRepository{db: db}
}

func (r *AdminRepository) GetByID(ctx context.Context, id uint) (*domain.Admin, error) {
	var admin domain.Admin
	err := r.db.WithContext(ctx).First(&admin, id).Error
	if errors.Is(err, gorm.ErrRecordNotFound) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &admin, nil
}

func (r *AdminRepository) GetByTelegramID(ctx context.Context, telegramID int64) (*domain.Admin, error) {
	var admin domain.Admin
	err := r.db.WithContext(ctx).Where("telegram_id = ?", telegramID).First(&admin).Error
	if errors.Is(err, gorm.ErrRecordNotFound) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &admin, nil
}

func (r *AdminRepository) GetByFirstName(ctx context.Context, firstName string) (*domain.Admin, error) {
	var admin domain.Admin
	err := r.db.WithContext(ctx).Where("first_name = ?", firstName).First(&admin).Error
	if errors.Is(err, gorm.ErrRecordNotFound) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &admin, nil
}

func (r *AdminRepository) ListActiveWithTelegram(ctx context.Context) ([]domain.Admin, error) {
	var admins []domain.Admin
	err := r.db.WithContext(ctx).
		Where("is_active = ? AND telegram_id IS NOT NULL AND telegram_id > 0", true).
		Find(&admins).Error
	if err != nil {
		return nil, err
	}
	return admins, nil
}

func (r *AdminRepository) Create(ctx context.Context, admin *domain.Admin) error {
	return r.db.WithContext(ctx).Create(admin).Error
}

func (r *AdminRepository) Update(ctx context.Context, admin *domain.Admin) error {
	return r.db.WithContext(ctx).Save(admin).Error
}
