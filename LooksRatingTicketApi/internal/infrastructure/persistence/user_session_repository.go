package persistence

import (
	"context"
	"errors"

	"gorm.io/gorm"
	"gorm.io/gorm/clause"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

type UserSessionRepository struct {
	db *gorm.DB
}

func NewUserSessionRepository(db *gorm.DB) contracts.UserSessionRepository {
	return &UserSessionRepository{db: db}
}

func (r *UserSessionRepository) GetByTelegramID(ctx context.Context, telegramID int64) (*domain.UserSession, error) {
	var session domain.UserSession
	err := r.db.WithContext(ctx).
		Preload("Admin").
		Where("telegram_id = ?", telegramID).
		First(&session).Error
	if errors.Is(err, gorm.ErrRecordNotFound) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &session, nil
}

func (r *UserSessionRepository) GetByTelegramIDForUpdate(ctx context.Context, telegramID int64) (*domain.UserSession, error) {
	return r.loadForUpdate(r.db.WithContext(ctx), telegramID)
}

func (r *UserSessionRepository) ExistsByTelegramID(ctx context.Context, telegramID int64) (bool, error) {
	var count int64
	err := r.db.WithContext(ctx).
		Model(&domain.UserSession{}).
		Where("telegram_id = ?", telegramID).
		Count(&count).Error
	return count > 0, err
}

func (r *UserSessionRepository) Create(ctx context.Context, session *domain.UserSession) error {
	return r.db.WithContext(ctx).Create(session).Error
}

func (r *UserSessionRepository) Update(ctx context.Context, session *domain.UserSession) error {
	return r.db.WithContext(ctx).Save(session).Error
}

func (r *UserSessionRepository) Mutate(
	ctx context.Context,
	telegramID int64,
	fn func(*domain.UserSession) error,
) (*domain.UserSession, error) {
	var updated domain.UserSession
	err := r.db.WithContext(ctx).Transaction(func(tx *gorm.DB) error {
		session, err := r.loadForUpdate(tx, telegramID)
		if err != nil {
			return err
		}
		if session == nil {
			return domain.ErrSessionNotFound
		}
		if err := fn(session); err != nil {
			return err
		}
		if err := tx.Save(session).Error; err != nil {
			return err
		}
		updated = *session
		return nil
	})
	if err != nil {
		return nil, err
	}
	return &updated, nil
}

func (r *UserSessionRepository) BindAdminLogin(
	ctx context.Context,
	telegramID int64,
	adminID uint,
) (*domain.UserSession, error) {
	var updated domain.UserSession
	err := r.db.WithContext(ctx).Transaction(func(tx *gorm.DB) error {
		session, err := r.loadForUpdate(tx, telegramID)
		if err != nil {
			return err
		}
		if session == nil {
			return domain.ErrSessionNotFound
		}

		var admin domain.Admin
		if err := tx.
			Clauses(clause.Locking{Strength: "UPDATE"}).
			First(&admin, adminID).Error; err != nil {
			if errors.Is(err, gorm.ErrRecordNotFound) {
				return domain.ErrAdminNotFound
			}
			return err
		}
		if !admin.IsActive {
			return domain.ErrInvalidPassword
		}
		if err := admin.BindTelegram(telegramID); err != nil {
			return err
		}
		if err := session.LinkAdmin(admin.ID); err != nil {
			return err
		}
		if err := tx.Save(&admin).Error; err != nil {
			if isDuplicateKey(err) {
				return domain.ErrInvalidPassword
			}
			return err
		}
		if err := tx.Save(session).Error; err != nil {
			return err
		}
		updated = *session
		return nil
	})
	if err != nil {
		return nil, err
	}
	return &updated, nil
}

func (r *UserSessionRepository) loadForUpdate(db *gorm.DB, telegramID int64) (*domain.UserSession, error) {
	var session domain.UserSession
	err := db.
		Clauses(clause.Locking{Strength: "UPDATE"}).
		Where("telegram_id = ?", telegramID).
		First(&session).Error
	if errors.Is(err, gorm.ErrRecordNotFound) {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &session, nil
}
