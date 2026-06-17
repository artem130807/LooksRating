package domain

import "time"

type MonitorCheckRun struct {
	ID            uint      `gorm:"primaryKey"`
	CheckedAt     time.Time `gorm:"column:checked_at;not null;index"`
	OverallStatus string    `gorm:"column:overall_status;size:16;not null"`
	ChecksJSON    string    `gorm:"column:checks_json;type:jsonb;not null"`
	CreatedAt     time.Time `gorm:"column:created_at;not null"`
}

func (MonitorCheckRun) TableName() string {
	return "monitor_check_runs"
}
