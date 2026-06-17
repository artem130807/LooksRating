package domain

import (
	"errors"
	"time"
)

type Admin struct {
	ID           uint      `gorm:"primaryKey"`
	TelegramID   *int64    `gorm:"uniqueIndex"`
	FirstName    string    `gorm:"column:first_name;size:128;not null;uniqueIndex"`
	PasswordHash string    `gorm:"column:password_hash;size:255;not null"`
	IsActive     bool      `gorm:"column:is_active;not null;default:true"`
	CreatedAt    time.Time `gorm:"column:created_at;not null"`
	UpdatedAt    time.Time `gorm:"column:updated_at;not null"`
}

func (Admin) TableName() string {
	return "ticket_admins"
}

func NewAdmin(firstName, passwordHash string, telegramID *int64) (*Admin, error) {
	if firstName == "" {
		return nil, errors.New("имя администратора обязательно")
	}
	if passwordHash == "" {
		return nil, errors.New("хэш пароля обязателен")
	}

	now := time.Now().UTC()
	return &Admin{
		FirstName:    firstName,
		PasswordHash: passwordHash,
		TelegramID:   telegramID,
		IsActive:     true,
		CreatedAt:    now,
		UpdatedAt:    now,
	}, nil
}

func (a *Admin) BindTelegram(telegramID int64) error {
	if telegramID <= 0 {
		return ErrInvalidTelegramID
	}
	a.TelegramID = &telegramID
	a.UpdatedAt = time.Now().UTC()
	return nil
}

func (a *Admin) Deactivate() {
	a.IsActive = false
	a.UpdatedAt = time.Now().UTC()
}
