package moderation

import (
	"context"
	"strings"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

type Service struct {
	tickets contracts.TicketClient
	actions contracts.ModerationActionsClient
	sessions contracts.UserSessionRepository
}

func NewService(
	tickets contracts.TicketClient,
	actions contracts.ModerationActionsClient,
	sessions contracts.UserSessionRepository,
) *Service {
	return &Service{
		tickets:  tickets,
		actions:  actions,
		sessions: sessions,
	}
}

func (s *Service) BeginCitySelection(ctx context.Context, telegramID int64) ([]string, error) {
	if err := s.requireAuthenticated(ctx, telegramID); err != nil {
		return nil, err
	}

	cities, err := s.tickets.ListModerationCities(ctx)
	if err != nil {
		return nil, err
	}

	_, err = s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		return session.SetState(domain.AdminSessionStateAwaitingCity)
	})
	if err != nil {
		return nil, err
	}

	return cities, nil
}

func (s *Service) SelectCity(ctx context.Context, telegramID int64, city string) (int, error) {
	if err := s.requireAuthenticated(ctx, telegramID); err != nil {
		return 0, err
	}
	city = strings.TrimSpace(city)
	if city == "" {
		return 0, domain.ErrInvalidRequest
	}

	resolvedCity, totalCount, err := s.tickets.CountTicketsByCity(ctx, city)
	if err != nil {
		return 0, err
	}
	if totalCount == 0 {
		return 0, nil
	}

	_, err = s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		session.Payload = writeQueue(session.Payload, queueSnapshot{
			city:  resolvedCity,
			index: 0,
			total: totalCount,
		})
		return session.SetState(domain.AdminSessionStateModerating)
	})
	if err != nil {
		return 0, err
	}

	return totalCount, nil
}

func (s *Service) GetCurrentView(ctx context.Context, telegramID int64) (*CurrentTicketView, error) {
	const maxStaleSkips = 10

	for attempt := 0; attempt < maxStaleSkips; attempt++ {
		snap, err := s.loadQueue(ctx, telegramID)
		if err != nil {
			return nil, err
		}
		if snap.remaining() == 0 {
			return nil, domain.ErrTicketQueueEmpty
		}

		queued, err := s.tickets.GetQueuedTicket(ctx, snap.city, snap.index)
		if err != nil {
			return nil, err
		}

		if queued.TotalCount == 0 || snap.index >= queued.TotalCount {
			if refreshErr := s.refreshQueue(ctx, telegramID, snap.city); refreshErr != nil {
				return nil, refreshErr
			}
			continue
		}

		if queued.ResolvedCity != "" && queued.ResolvedCity != snap.city {
			if syncErr := s.syncQueueCity(ctx, telegramID, queued.ResolvedCity); syncErr != nil {
				return nil, syncErr
			}
			snap.city = queued.ResolvedCity
		}

		if queued.TotalCount > 0 && queued.TotalCount != snap.total {
			if syncErr := s.syncQueueTotal(ctx, telegramID, queued.TotalCount); syncErr != nil {
				return nil, syncErr
			}
			snap.total = queued.TotalCount
		}

		if queued.Ticket == nil {
			if advanceErr := s.advanceAfterRemoval(ctx, telegramID); advanceErr != nil {
				return nil, advanceErr
			}
			continue
		}

		return &CurrentTicketView{
			City:      snap.city,
			Remaining: snap.remaining(),
			Ticket:    queued.Ticket,
		}, nil
	}

	return nil, domain.ErrTicketQueueEmpty
}

func (s *Service) DismissCurrent(ctx context.Context, telegramID int64) error {
	ticketID, err := s.resolveCurrentTicketID(ctx, telegramID)
	if err != nil {
		return err
	}

	result, err := s.actions.RejectTicketPhotoProfile(ctx, ticketID)
	if err != nil {
		return err
	}
	if err := applyModerationResult(result); err != nil {
		return err
	}
	return s.advanceAfterRemoval(ctx, telegramID)
}

func (s *Service) DeleteCurrentProfile(ctx context.Context, telegramID int64) error {
	photoProfileID, err := s.resolveCurrentPhotoProfileID(ctx, telegramID)
	if err != nil {
		return err
	}

	result, err := s.actions.RemoveTicketsPhotoprofile(ctx, photoProfileID)
	if err != nil {
		return err
	}
	if err := applyModerationResult(result); err != nil {
		return err
	}
	return s.advanceAfterRemoval(ctx, telegramID)
}

func (s *Service) DeleteCurrentUserAccount(ctx context.Context, telegramID int64) error {
	photoProfileID, err := s.resolveCurrentPhotoProfileID(ctx, telegramID)
	if err != nil {
		return err
	}

	result, err := s.actions.RemoveTicketsPhotoprofile(ctx, photoProfileID)
	if err != nil {
		return err
	}
	if err := applyModerationResult(result); err != nil {
		return err
	}
	return s.advanceAfterRemoval(ctx, telegramID)
}

func (s *Service) SkipCurrent(ctx context.Context, telegramID int64) error {
	if _, err := s.loadQueue(ctx, telegramID); err != nil {
		return err
	}
	return s.advanceAfterSkip(ctx, telegramID)
}

func (s *Service) resolveCurrentTicketID(ctx context.Context, telegramID int64) (string, error) {
	snap, err := s.loadQueue(ctx, telegramID)
	if err != nil {
		return "", err
	}
	if snap.remaining() == 0 {
		return "", domain.ErrTicketQueueEmpty
	}

	queued, err := s.tickets.GetQueuedTicket(ctx, snap.city, snap.index)
	if err != nil {
		return "", err
	}
	if queued.Ticket == nil || queued.Ticket.ID == "" {
		return "", domain.ErrTicketNotFound
	}
	return queued.Ticket.ID, nil
}

func (s *Service) resolveCurrentPhotoProfileID(ctx context.Context, telegramID int64) (string, error) {
	snap, err := s.loadQueue(ctx, telegramID)
	if err != nil {
		return "", err
	}
	if snap.remaining() == 0 {
		return "", domain.ErrTicketQueueEmpty
	}

	queued, err := s.tickets.GetQueuedTicket(ctx, snap.city, snap.index)
	if err != nil {
		return "", err
	}
	if queued.Ticket == nil || queued.Ticket.PhotoProfileID == "" {
		return "", domain.ErrTicketNotFound
	}
	return queued.Ticket.PhotoProfileID, nil
}

func applyModerationResult(result *contracts.ModerationActionResult) error {
	if result == nil {
		return domain.ErrUpstreamUnavailable
	}
	if result.IsSuccess {
		return nil
	}
	return &domain.ModerationActionError{Message: result.Message}
}

func (s *Service) loadQueue(ctx context.Context, telegramID int64) (queueSnapshot, error) {
	session, err := s.sessions.GetByTelegramID(ctx, telegramID)
	if err != nil {
		return queueSnapshot{}, err
	}
	if session == nil {
		return queueSnapshot{}, domain.ErrSessionNotFound
	}
	if session.State != domain.AdminSessionStateModerating {
		return queueSnapshot{}, domain.ErrInvalidState
	}
	if !session.IsAuthenticated() {
		return queueSnapshot{}, domain.ErrAdminNotAuthenticated
	}
	return readQueue(session.Payload), nil
}

func (s *Service) requireAuthenticated(ctx context.Context, telegramID int64) error {
	session, err := s.sessions.GetByTelegramID(ctx, telegramID)
	if err != nil {
		return err
	}
	if session == nil {
		return domain.ErrSessionNotFound
	}
	if !session.IsAuthenticated() {
		return domain.ErrAdminNotAuthenticated
	}
	return nil
}

func (s *Service) syncQueueTotal(ctx context.Context, telegramID int64, total int) error {
	_, err := s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		locked := readQueue(session.Payload)
		locked.total = total
		session.Payload = writeQueue(session.Payload, locked)
		return nil
	})
	return err
}

func (s *Service) syncQueueCity(ctx context.Context, telegramID int64, city string) error {
	_, err := s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		locked := readQueue(session.Payload)
		locked.city = city
		session.Payload = writeQueue(session.Payload, locked)
		return nil
	})
	return err
}

func (s *Service) refreshQueue(ctx context.Context, telegramID int64, city string) error {
	resolvedCity, totalCount, err := s.tickets.CountTicketsByCity(ctx, city)
	if err != nil {
		return err
	}

	_, err = s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		if totalCount == 0 {
			session.Payload = writeQueue(session.Payload, queueSnapshot{})
			return session.SetState(domain.AdminSessionStateAwaitingCity)
		}
		session.Payload = writeQueue(session.Payload, queueSnapshot{
			city:  resolvedCity,
			index: 0,
			total: totalCount,
		})
		return nil
	})
	return err
}

func (s *Service) advanceAfterSkip(ctx context.Context, telegramID int64) error {
	snap, err := s.loadQueue(ctx, telegramID)
	if err != nil {
		return err
	}

	_, totalCount, err := s.tickets.CountTicketsByCity(ctx, snap.city)
	if err != nil {
		return err
	}

	nextIndex := snap.index + 1
	return s.persistQueuePosition(ctx, telegramID, snap.city, nextIndex, totalCount)
}

func (s *Service) advanceAfterRemoval(ctx context.Context, telegramID int64) error {
	snap, err := s.loadQueue(ctx, telegramID)
	if err != nil {
		return err
	}

	_, totalCount, err := s.tickets.CountTicketsByCity(ctx, snap.city)
	if err != nil {
		return err
	}

	return s.persistQueuePosition(ctx, telegramID, snap.city, snap.index, totalCount)
}

func (s *Service) persistQueuePosition(ctx context.Context, telegramID int64, city string, index, total int) error {
	if total == 0 || index >= total {
		_, err := s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
			session.Payload = writeQueue(session.Payload, queueSnapshot{})
			return session.SetState(domain.AdminSessionStateAwaitingCity)
		})
		return err
	}

	_, err := s.sessions.Mutate(ctx, telegramID, func(session *domain.UserSession) error {
		session.Payload = writeQueue(session.Payload, queueSnapshot{
			city:  city,
			index: index,
			total: total,
		})
		return nil
	})
	return err
}
