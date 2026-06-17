package persistence

import (
	"context"
	"time"

	"gorm.io/gorm"

	"looksrating/ticketapi/internal/domain"
)

type MonitorRepository struct {
	db *gorm.DB
}

func NewMonitorRepository(db *gorm.DB) *MonitorRepository {
	return &MonitorRepository{db: db}
}

func (r *MonitorRepository) SaveCheckRun(ctx context.Context, run *domain.MonitorCheckRun) error {
	return r.db.WithContext(ctx).Create(run).Error
}

func (r *MonitorRepository) GetLatestCheckRun(ctx context.Context) (*domain.MonitorCheckRun, error) {
	var run domain.MonitorCheckRun
	err := r.db.WithContext(ctx).Order("checked_at DESC").First(&run).Error
	if err == gorm.ErrRecordNotFound {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &run, nil
}

func (r *MonitorRepository) GetAlertByID(ctx context.Context, id uint) (*domain.MonitorAlert, error) {
	var alert domain.MonitorAlert
	err := r.db.WithContext(ctx).Where("id = ?", id).First(&alert).Error
	if err == gorm.ErrRecordNotFound {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &alert, nil
}

func (r *MonitorRepository) GetAlertByFingerprint(ctx context.Context, fingerprint string) (*domain.MonitorAlert, error) {
	var alert domain.MonitorAlert
	err := r.db.WithContext(ctx).Where("fingerprint = ?", fingerprint).First(&alert).Error
	if err == gorm.ErrRecordNotFound {
		return nil, nil
	}
	if err != nil {
		return nil, err
	}
	return &alert, nil
}

func (r *MonitorRepository) SaveAlert(ctx context.Context, alert *domain.MonitorAlert) error {
	return r.db.WithContext(ctx).Save(alert).Error
}

func (r *MonitorRepository) ListOpenAlerts(ctx context.Context) ([]domain.MonitorAlert, error) {
	var alerts []domain.MonitorAlert
	err := r.db.WithContext(ctx).
		Where(
			"status = ? AND fingerprint NOT LIKE ?",
			domain.MonitorAlertStatusOpen,
			"%:recovered",
		).
		Order("first_seen_at DESC").
		Find(&alerts).Error
	return alerts, err
}

func (r *MonitorRepository) ListPendingAlerts(ctx context.Context, cooldownSince time.Time) ([]domain.MonitorAlert, error) {
	var alerts []domain.MonitorAlert
	err := r.db.WithContext(ctx).
		Where(
			"status = ? AND (last_notified_at IS NULL OR last_notified_at < ?)",
			domain.MonitorAlertStatusOpen,
			cooldownSince,
		).
		Order("first_seen_at ASC").
		Find(&alerts).Error
	return alerts, err
}

func (r *MonitorRepository) MarkAlertNotified(ctx context.Context, id uint, notifiedAt time.Time) error {
	return r.db.WithContext(ctx).Model(&domain.MonitorAlert{}).
		Where("id = ?", id).
		Updates(map[string]any{
			"last_notified_at": notifiedAt,
			"updated_at":       notifiedAt,
		}).Error
}
