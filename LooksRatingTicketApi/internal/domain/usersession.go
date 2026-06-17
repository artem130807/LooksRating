package domain

import (
	"database/sql/driver"
	"encoding/json"
	"fmt"
	"time"

	"github.com/google/uuid"
)

type SessionPayload map[string]string

func (p SessionPayload) Value() (driver.Value, error) {
	if len(p) == 0 {
		return "{}", nil
	}
	b, err := json.Marshal(p)
	if err != nil {
		return nil, err
	}
	return string(b), nil
}

func (p *SessionPayload) Scan(value any) error {
	if value == nil {
		*p = SessionPayload{}
		return nil
	}

	var data []byte
	switch v := value.(type) {
	case string:
		data = []byte(v)
	case []byte:
		data = v
	default:
		return fmt.Errorf("неподдерживаемый тип payload: %T", value)
	}

	if len(data) == 0 {
		*p = SessionPayload{}
		return nil
	}

	return json.Unmarshal(data, p)
}

// UserSession — сессия администратора в Telegram-боте жалоб.
// Отдельна от UserSession основного LooksRatingApi (пользовательский бот).
type UserSession struct {
	ID         uuid.UUID         `gorm:"type:uuid;primaryKey"`
	TelegramID int64             `gorm:"column:telegram_id;uniqueIndex;not null"`
	AdminID    *uint             `gorm:"column:admin_id;index"`
	Admin      *Admin            `gorm:"foreignKey:AdminID;references:ID;constraint:OnDelete:SET NULL"`
	State      AdminSessionState `gorm:"column:state;size:64;not null"`
	Payload    SessionPayload    `gorm:"column:payload;type:jsonb;not null;default:'{}'"`
	UpdatedAt  time.Time         `gorm:"column:updated_at;not null"`
}

func (UserSession) TableName() string {
	return "ticket_user_sessions"
}

func NewUserSession(telegramID int64) (*UserSession, error) {
	if telegramID <= 0 {
		return nil, ErrInvalidTelegramID
	}

	now := time.Now().UTC()
	return &UserSession{
		ID:         uuid.New(),
		TelegramID: telegramID,
		State:      AdminSessionStateStart,
		Payload:    SessionPayload{},
		UpdatedAt:  now,
	}, nil
}

func (s *UserSession) SetState(state AdminSessionState) error {
	if !state.IsValid() {
		return ErrInvalidState
	}
	s.State = state
	s.UpdatedAt = time.Now().UTC()
	return nil
}

func (s *UserSession) LinkAdmin(adminID uint) error {
	if adminID == 0 {
		return ErrInvalidAdminID
	}
	s.AdminID = &adminID
	s.State = AdminSessionStateAuthenticated
	s.UpdatedAt = time.Now().UTC()
	return nil
}

func (s *UserSession) ClearAdmin() {
	s.AdminID = nil
	s.State = AdminSessionStateStart
	s.Payload = SessionPayload{}
	s.UpdatedAt = time.Now().UTC()
}

func (s *UserSession) SetPayload(key, value string) {
	if s.Payload == nil {
		s.Payload = SessionPayload{}
	}
	if value == "" {
		delete(s.Payload, key)
	} else {
		s.Payload[key] = value
	}
	s.UpdatedAt = time.Now().UTC()
}

func (s *UserSession) GetPayload(key string) (string, bool) {
	if s.Payload == nil {
		return "", false
	}
	v, ok := s.Payload[key]
	return v, ok
}

func (s *UserSession) IsAuthenticated() bool {
	return s.AdminID != nil &&
		s.State != AdminSessionStateStart &&
		s.State != AdminSessionStateAwaitingLogin &&
		s.State != AdminSessionStateAwaitingPassword
}
