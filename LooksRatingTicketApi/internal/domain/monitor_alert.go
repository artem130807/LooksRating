package domain

import "time"

const (
	MonitorAlertStatusOpen     = "open"
	MonitorAlertStatusResolved = "resolved"

	MonitorAlertSeverityWarning = "warning"
	MonitorAlertSeverityCritical = "critical"
)

type MonitorAlert struct {
	ID              uint       `gorm:"primaryKey"`
	Fingerprint     string     `gorm:"column:fingerprint;size:128;not null;uniqueIndex"`
	Severity        string     `gorm:"column:severity;size:16;not null"`
	Title           string     `gorm:"column:title;size:256;not null"`
	Body            string     `gorm:"column:body;type:text;not null"`
	Status          string     `gorm:"column:status;size:16;not null;index"`
	FirstSeenAt     time.Time  `gorm:"column:first_seen_at;not null"`
	LastNotifiedAt  *time.Time `gorm:"column:last_notified_at"`
	ResolvedAt      *time.Time `gorm:"column:resolved_at"`
	UpdatedAt       time.Time  `gorm:"column:updated_at;not null"`
}

func (MonitorAlert) TableName() string {
	return "monitor_alerts"
}
