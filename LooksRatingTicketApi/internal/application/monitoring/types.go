package monitoring

import (
	"errors"
	"time"
)

var (
	ErrMonitoringDisabled = errors.New("мониторинг отключён")

	AllowedLogServices = map[string]struct{}{
		"api":          {},
		"bot":          {},
		"ticket-api":   {},
		"ticket-bot":   {},
		"tgifts-buyer": {},
	}

	MaxLogTailLines = 500
)

const (
	CheckStatusOK    = "ok"
	CheckStatusFail  = "fail"
	CheckStatusSkip  = "skip"
	OverallStatusOK  = "ok"
	OverallStatusFail = "fail"
)

type CheckResult struct {
	ID       string `json:"id"`
	Name     string `json:"name"`
	Status   string `json:"status"`
	Message  string `json:"message,omitempty"`
	Duration string `json:"duration"`
}

type RunResult struct {
	CheckedAt     time.Time     `json:"checkedAt"`
	OverallStatus string        `json:"overallStatus"`
	Checks        []CheckResult `json:"checks"`
}

type StatusResponse struct {
	LastRun *RunResult `json:"lastRun,omitempty"`
}

type AlertView struct {
	ID          uint   `json:"id"`
	Fingerprint string `json:"fingerprint"`
	Severity    string `json:"severity"`
	Title       string `json:"title"`
	Body        string `json:"body"`
	Status      string `json:"status"`
	FirstSeenAt string `json:"firstSeenAt"`
}

type PendingAlertsResponse struct {
	Alerts     []AlertView `json:"alerts"`
	Recipients []int64     `json:"recipients"`
}

type LogsResponse struct {
	Service string `json:"service"`
	Lines   string `json:"lines"`
	Enabled bool   `json:"enabled"`
}
