package monitoring

import (
	"context"
	"strings"
	"sync"
	"testing"
	"time"

	"looksrating/ticketapi/internal/domain"
)

type memoryMonitorRepo struct {
	mu      sync.Mutex
	alerts  map[string]*domain.MonitorAlert
	runs    []domain.MonitorCheckRun
	nextID  uint
}

func newMemoryMonitorRepo() *memoryMonitorRepo {
	return &memoryMonitorRepo{alerts: make(map[string]*domain.MonitorAlert)}
}

func (m *memoryMonitorRepo) SaveCheckRun(ctx context.Context, run *domain.MonitorCheckRun) error {
	m.mu.Lock()
	defer m.mu.Unlock()
	m.runs = append(m.runs, *run)
	return nil
}

func (m *memoryMonitorRepo) GetLatestCheckRun(ctx context.Context) (*domain.MonitorCheckRun, error) {
	m.mu.Lock()
	defer m.mu.Unlock()
	if len(m.runs) == 0 {
		return nil, nil
	}
	last := m.runs[len(m.runs)-1]
	return &last, nil
}

func (m *memoryMonitorRepo) GetAlertByID(ctx context.Context, id uint) (*domain.MonitorAlert, error) {
	m.mu.Lock()
	defer m.mu.Unlock()
	for _, alert := range m.alerts {
		if alert.ID == id {
			copy := *alert
			return &copy, nil
		}
	}
	return nil, nil
}

func (m *memoryMonitorRepo) GetAlertByFingerprint(ctx context.Context, fingerprint string) (*domain.MonitorAlert, error) {
	m.mu.Lock()
	defer m.mu.Unlock()
	alert, ok := m.alerts[fingerprint]
	if !ok {
		return nil, nil
	}
	copy := *alert
	return &copy, nil
}

func (m *memoryMonitorRepo) SaveAlert(ctx context.Context, alert *domain.MonitorAlert) error {
	m.mu.Lock()
	defer m.mu.Unlock()
	if alert.ID == 0 {
		m.nextID++
		alert.ID = m.nextID
	}
	copy := *alert
	m.alerts[alert.Fingerprint] = &copy
	return nil
}

func (m *memoryMonitorRepo) ListOpenAlerts(ctx context.Context) ([]domain.MonitorAlert, error) {
	m.mu.Lock()
	defer m.mu.Unlock()
	out := make([]domain.MonitorAlert, 0)
	for _, alert := range m.alerts {
		if alert.Status == domain.MonitorAlertStatusOpen && !strings.HasSuffix(alert.Fingerprint, ":recovered") {
			out = append(out, *alert)
		}
	}
	return out, nil
}

func (m *memoryMonitorRepo) ListPendingAlerts(ctx context.Context, cooldownSince time.Time) ([]domain.MonitorAlert, error) {
	m.mu.Lock()
	defer m.mu.Unlock()
	out := make([]domain.MonitorAlert, 0)
	for _, alert := range m.alerts {
		if alert.Status != domain.MonitorAlertStatusOpen {
			continue
		}
		if alert.LastNotifiedAt == nil || alert.LastNotifiedAt.Before(cooldownSince) {
			out = append(out, *alert)
		}
	}
	return out, nil
}

func (m *memoryMonitorRepo) MarkAlertNotified(ctx context.Context, id uint, notifiedAt time.Time) error {
	m.mu.Lock()
	defer m.mu.Unlock()
	for _, alert := range m.alerts {
		if alert.ID == id {
			alert.LastNotifiedAt = &notifiedAt
			alert.UpdatedAt = notifiedAt
			if strings.HasSuffix(alert.Fingerprint, ":recovered") {
				alert.Status = domain.MonitorAlertStatusResolved
				alert.ResolvedAt = &notifiedAt
			}
			return nil
		}
	}
	return nil
}

func TestAlertServiceOpenAndPendingCooldown(t *testing.T) {
	repo := newMemoryMonitorRepo()
	now := time.Date(2026, 6, 1, 12, 0, 0, 0, time.UTC)
	svc := NewAlertService(repo, 30*time.Minute)
	svc.now = func() time.Time { return now }

	ctx := context.Background()
	if err := svc.Open(ctx, "api_ready:down", domain.MonitorAlertSeverityCritical, "API ready", "down"); err != nil {
		t.Fatalf("open: %v", err)
	}

	pending, err := svc.ListPending(ctx)
	if err != nil || len(pending) != 1 {
		t.Fatalf("expected 1 pending, got %d err=%v", len(pending), err)
	}

	notified := now.Add(1 * time.Minute)
	if err := svc.Ack(ctx, pending[0].ID); err != nil {
		t.Fatalf("ack: %v", err)
	}
	svc.now = func() time.Time { return notified.Add(10 * time.Minute) }
	pending, err = svc.ListPending(ctx)
	if err != nil || len(pending) != 0 {
		t.Fatalf("expected no pending inside cooldown, got %d", len(pending))
	}

	svc.now = func() time.Time { return notified.Add(31 * time.Minute) }
	pending, err = svc.ListPending(ctx)
	if err != nil || len(pending) != 1 {
		t.Fatalf("expected pending after cooldown, got %d", len(pending))
	}
}

func TestAlertServiceResolveCreatesRecovery(t *testing.T) {
	repo := newMemoryMonitorRepo()
	svc := NewAlertService(repo, 30*time.Minute)
	ctx := context.Background()

	if err := svc.Open(ctx, "api_live:down", domain.MonitorAlertSeverityCritical, "API live", "fail"); err != nil {
		t.Fatalf("open: %v", err)
	}
	if err := svc.Resolve(ctx, "api_live:down", "API live"); err != nil {
		t.Fatalf("resolve: %v", err)
	}

	original, err := repo.GetAlertByFingerprint(ctx, "api_live:down")
	if err != nil || original == nil || original.Status != domain.MonitorAlertStatusResolved {
		t.Fatalf("original alert not resolved: %#v err=%v", original, err)
	}

	recovery, err := repo.GetAlertByFingerprint(ctx, "api_live:down:recovered")
	if err != nil || recovery == nil || recovery.Status != domain.MonitorAlertStatusOpen {
		t.Fatalf("recovery alert missing: %#v err=%v", recovery, err)
	}
}

func TestAlertServiceAckResolvesRecovery(t *testing.T) {
	repo := newMemoryMonitorRepo()
	svc := NewAlertService(repo, 30*time.Minute)
	ctx := context.Background()

	if err := svc.Open(ctx, "api_live:down", domain.MonitorAlertSeverityCritical, "API live", "fail"); err != nil {
		t.Fatalf("open: %v", err)
	}
	if err := svc.Resolve(ctx, "api_live:down", "API live"); err != nil {
		t.Fatalf("resolve: %v", err)
	}

	recovery, err := repo.GetAlertByFingerprint(ctx, "api_live:down:recovered")
	if err != nil || recovery == nil {
		t.Fatalf("recovery missing: %v", err)
	}
	if err := svc.Ack(ctx, recovery.ID); err != nil {
		t.Fatalf("ack: %v", err)
	}

	recovery, err = repo.GetAlertByFingerprint(ctx, "api_live:down:recovered")
	if err != nil || recovery == nil || recovery.Status != domain.MonitorAlertStatusResolved {
		t.Fatalf("recovery should be resolved after ack, got %#v", recovery)
	}

	open, err := svc.ListOpen(ctx)
	if err != nil {
		t.Fatalf("list open: %v", err)
	}
	for _, alert := range open {
		if strings.HasSuffix(alert.Fingerprint, ":recovered") {
			t.Fatalf("recovery alert should not be listed as open: %#v", alert)
		}
	}
}

func TestQuartzSeasonWindow(t *testing.T) {
	loc, err := time.LoadLocation("Europe/Moscow")
	if err != nil {
		t.Fatalf("location: %v", err)
	}
	rule := defaultQuartzRules()[0]
	inWindow := rule.Window(time.Date(2026, 6, 1, 2, 30, 0, 0, loc), loc)
	if !inWindow {
		t.Fatal("expected season window on 1st at 02:30 MSK")
	}
	outWindow := rule.Window(time.Date(2026, 6, 2, 2, 30, 0, 0, loc), loc)
	if outWindow {
		t.Fatal("expected no season window on 2nd")
	}
}

func TestSanitizeLogOutputRedactsApiKey(t *testing.T) {
	raw := "request headers X-Api-Key: super_secret_value\n"
	out := sanitizeLogOutput(raw)
	if out == raw {
		t.Fatalf("expected redaction, got %q", out)
	}
}
