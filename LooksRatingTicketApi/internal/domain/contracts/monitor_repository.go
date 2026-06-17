package contracts

import (
	"context"
	"time"

	"looksrating/ticketapi/internal/domain"
)

type MonitorRepository interface {
	SaveCheckRun(ctx context.Context, run *domain.MonitorCheckRun) error
	GetLatestCheckRun(ctx context.Context) (*domain.MonitorCheckRun, error)

	GetAlertByID(ctx context.Context, id uint) (*domain.MonitorAlert, error)
	GetAlertByFingerprint(ctx context.Context, fingerprint string) (*domain.MonitorAlert, error)
	SaveAlert(ctx context.Context, alert *domain.MonitorAlert) error
	ListOpenAlerts(ctx context.Context) ([]domain.MonitorAlert, error)
	ListPendingAlerts(ctx context.Context, cooldownSince time.Time) ([]domain.MonitorAlert, error)
	MarkAlertNotified(ctx context.Context, id uint, notifiedAt time.Time) error
}
