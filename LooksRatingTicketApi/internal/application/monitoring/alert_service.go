package monitoring

import (
	"context"
	"errors"
	"fmt"
	"strings"
	"time"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

var ErrAlertNotFound = errors.New("алерт не найден")

type AlertService struct {
	repo     contracts.MonitorRepository
	cooldown time.Duration
	now      func() time.Time
}

func NewAlertService(repo contracts.MonitorRepository, cooldown time.Duration) *AlertService {
	return &AlertService{
		repo:     repo,
		cooldown: cooldown,
		now:      time.Now,
	}
}

func (s *AlertService) Open(ctx context.Context, fingerprint, severity, title, body string) error {
	now := s.now().UTC()
	existing, err := s.repo.GetAlertByFingerprint(ctx, fingerprint)
	if err != nil {
		return err
	}

	if existing != nil {
		if existing.Status == domain.MonitorAlertStatusOpen {
			existing.Body = body
			existing.UpdatedAt = now
			return s.repo.SaveAlert(ctx, existing)
		}
		existing.Status = domain.MonitorAlertStatusOpen
		existing.Severity = severity
		existing.Title = title
		existing.Body = body
		existing.FirstSeenAt = now
		existing.ResolvedAt = nil
		existing.LastNotifiedAt = nil
		existing.UpdatedAt = now
		return s.repo.SaveAlert(ctx, existing)
	}

	alert := &domain.MonitorAlert{
		Fingerprint: fingerprint,
		Severity:    severity,
		Title:       title,
		Body:        body,
		Status:      domain.MonitorAlertStatusOpen,
		FirstSeenAt: now,
		UpdatedAt:   now,
	}
	return s.repo.SaveAlert(ctx, alert)
}

func (s *AlertService) Resolve(ctx context.Context, fingerprint, title string) error {
	existing, err := s.repo.GetAlertByFingerprint(ctx, fingerprint)
	if err != nil {
		return err
	}
	if existing == nil || existing.Status != domain.MonitorAlertStatusOpen {
		return nil
	}

	now := s.now().UTC()
	existing.Status = domain.MonitorAlertStatusResolved
	existing.ResolvedAt = &now
	existing.UpdatedAt = now
	if err := s.repo.SaveAlert(ctx, existing); err != nil {
		return err
	}

	return s.createRecoveryAlert(ctx, fingerprint, title, now)
}

func (s *AlertService) createRecoveryAlert(ctx context.Context, fingerprint, title string, now time.Time) error {
	recoveryFP := fingerprint + ":recovered"
	recovery, err := s.repo.GetAlertByFingerprint(ctx, recoveryFP)
	if err != nil {
		return err
	}

	recoveryAlert := &domain.MonitorAlert{
		Fingerprint: recoveryFP,
		Severity:    domain.MonitorAlertSeverityWarning,
		Title:       title + " — восстановлено",
		Body:        fmt.Sprintf("Проверка %s снова в норме.", fingerprint),
		Status:      domain.MonitorAlertStatusOpen,
		FirstSeenAt: now,
		UpdatedAt:   now,
	}
	if recovery != nil {
		recoveryAlert.ID = recovery.ID
	}
	return s.repo.SaveAlert(ctx, recoveryAlert)
}

func (s *AlertService) ListOpen(ctx context.Context) ([]domain.MonitorAlert, error) {
	return s.repo.ListOpenAlerts(ctx)
}

func (s *AlertService) ListPending(ctx context.Context) ([]domain.MonitorAlert, error) {
	cooldownSince := s.now().UTC().Add(-s.cooldown)
	return s.repo.ListPendingAlerts(ctx, cooldownSince)
}

func (s *AlertService) Ack(ctx context.Context, id uint) error {
	alert, err := s.repo.GetAlertByID(ctx, id)
	if err != nil {
		return err
	}
	if alert == nil {
		return ErrAlertNotFound
	}

	now := s.now().UTC()
	if err := s.repo.MarkAlertNotified(ctx, id, now); err != nil {
		return err
	}

	if !strings.HasSuffix(alert.Fingerprint, ":recovered") {
		return nil
	}

	alert.LastNotifiedAt = &now
	alert.Status = domain.MonitorAlertStatusResolved
	alert.ResolvedAt = &now
	alert.UpdatedAt = now
	return s.repo.SaveAlert(ctx, alert)
}

func MapAlertView(alert domain.MonitorAlert) AlertView {
	return AlertView{
		ID:          alert.ID,
		Fingerprint: alert.Fingerprint,
		Severity:    alert.Severity,
		Title:       alert.Title,
		Body:        alert.Body,
		Status:      alert.Status,
		FirstSeenAt: alert.FirstSeenAt.UTC().Format(time.RFC3339),
	}
}
