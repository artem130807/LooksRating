package session

import (
	"context"
	"errors"
	"fmt"
	"strings"

	"github.com/jackc/pgx/v5/pgconn"
	"gorm.io/gorm"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

type Service struct {
	sessions contracts.UserSessionRepository
	admins   contracts.AdminRepository
	hasher   contracts.PasswordHasher
}

func NewService(
	sessions contracts.UserSessionRepository,
	admins contracts.AdminRepository,
	hasher contracts.PasswordHasher,
) *Service {
	return &Service{
		sessions: sessions,
		admins:   admins,
		hasher:   hasher,
	}
}

func (s *Service) Ensure(ctx context.Context, telegramID int64) (*domain.UserSession, error) {
	if telegramID <= 0 {
		return nil, domain.ErrInvalidTelegramID
	}

	session, err := s.sessions.GetByTelegramID(ctx, telegramID)
	if err != nil {
		return nil, err
	}
	if session != nil {
		return session, nil
	}

	session, err = domain.NewUserSession(telegramID)
	if err != nil {
		return nil, err
	}
	if err := s.sessions.Create(ctx, session); err != nil {
		if isDuplicateKey(err) {
			existing, getErr := s.sessions.GetByTelegramID(ctx, telegramID)
			if getErr != nil {
				return nil, getErr
			}
			if existing == nil {
				return nil, err
			}
			return existing, nil
		}
		return nil, err
	}
	return session, nil
}

func isDuplicateKey(err error) bool {
	if errors.Is(err, gorm.ErrDuplicatedKey) {
		return true
	}
	var pgErr *pgconn.PgError
	if errors.As(err, &pgErr) {
		return pgErr.Code == "23505"
	}
	return false
}

func (s *Service) Get(ctx context.Context, telegramID int64) (*domain.UserSession, error) {
	session, err := s.sessions.GetByTelegramID(ctx, telegramID)
	if err != nil {
		return nil, err
	}
	if session == nil {
		return nil, domain.ErrSessionNotFound
	}
	return session, nil
}

func (s *Service) BeginLogin(ctx context.Context, telegramID int64) (*domain.UserSession, error) {
	if _, err := s.Ensure(ctx, telegramID); err != nil {
		return nil, err
	}
	return s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		session.SetPayload("login", "")
		return session.SetState(domain.AdminSessionStateAwaitingLogin)
	})
}

func (s *Service) SubmitLogin(ctx context.Context, telegramID int64, login string) (*domain.UserSession, error) {
	login = strings.TrimSpace(login)
	if login == "" {
		return nil, domain.ErrInvalidRequest
	}
	if _, err := s.Ensure(ctx, telegramID); err != nil {
		return nil, err
	}
	return s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		if session.State != domain.AdminSessionStateAwaitingLogin &&
			session.State != domain.AdminSessionStateAwaitingPassword {
			return domain.ErrInvalidState
		}
		session.SetPayload("login", login)
		return session.SetState(domain.AdminSessionStateAwaitingPassword)
	})
}

func (s *Service) Authenticate(ctx context.Context, telegramID int64, login, password string) (*domain.UserSession, error) {
	admin, err := s.admins.GetByFirstName(ctx, login)
	if err != nil {
		return nil, err
	}
	if admin == nil || !admin.IsActive {
		return nil, domain.ErrInvalidPassword
	}
	if err := s.hasher.Verify(admin.PasswordHash, password); err != nil {
		return nil, domain.ErrInvalidPassword
	}

	return s.sessions.BindAdminLogin(ctx, telegramID, admin.ID)
}

func (s *Service) SetState(ctx context.Context, telegramID int64, state domain.AdminSessionState) (*domain.UserSession, error) {
	return s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		return session.SetState(state)
	})
}

func (s *Service) SetPayload(ctx context.Context, telegramID int64, key, value string) (*domain.UserSession, error) {
	return s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		session.SetPayload(key, value)
		return nil
	})
}

func (s *Service) Logout(ctx context.Context, telegramID int64) error {
	_, err := s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		session.ClearAdmin()
		return nil
	})
	return err
}

func (s *Service) RequireAuthenticated(ctx context.Context, telegramID int64) error {
	session, err := s.Get(ctx, telegramID)
	if err != nil {
		return err
	}
	if !session.IsAuthenticated() {
		return domain.ErrAdminNotAuthenticated
	}
	return nil
}

func (s *Service) RegisterAdmin(ctx context.Context, firstName, plainPassword string, telegramID *int64) (*domain.Admin, error) {
	if firstName == "" || plainPassword == "" {
		return nil, fmt.Errorf("логин и пароль обязательны")
	}

	existing, err := s.admins.GetByFirstName(ctx, firstName)
	if err != nil {
		return nil, err
	}
	if existing != nil {
		return nil, errors.New("администратор с таким логином уже существует")
	}

	hash, err := s.hasher.Hash(plainPassword)
	if err != nil {
		return nil, err
	}

	admin, err := domain.NewAdmin(firstName, hash, telegramID)
	if err != nil {
		return nil, err
	}
	if err := s.admins.Create(ctx, admin); err != nil {
		return nil, err
	}
	return admin, nil
}
