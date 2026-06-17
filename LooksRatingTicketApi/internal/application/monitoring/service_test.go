package monitoring

import (
	"context"
	"net/http"
	"net/http/httptest"
	"testing"
	"time"

	"looksrating/ticketapi/internal/domain"
)

func TestMonitorServiceSmokeChecks(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		switch r.URL.Path {
		case "/health/live", "/health/ready":
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`{"status":"ok"}`))
		case "/api/cities":
			if r.Header.Get("X-Api-Key") != "test-key" {
				w.WriteHeader(http.StatusUnauthorized)
				return
			}
			w.WriteHeader(http.StatusOK)
			_, _ = w.Write([]byte(`[{"name":"Moscow"}]`))
		default:
			w.WriteHeader(http.StatusNotFound)
		}
	}))
	defer server.Close()

	repo := newMemoryMonitorRepo()
	alerts := NewAlertService(repo, time.Minute)
	svc := NewService(
		Config{
			Enabled:             true,
			LooksRatingHTTPBase: server.URL,
			LooksRatingAPIKey:   "test-key",
			TicketAPIHealthURL:  server.URL + "/health/live",
			TgiftsEnabled:       false,
		},
		repo,
		nil,
		alerts,
		NewLogTailService(LogTailConfig{Enabled: false}, nil),
		nil,
		2*time.Second,
	)

	result, err := svc.Run(context.Background())
	if err != nil {
		t.Fatalf("run: %v", err)
	}

	byID := map[string]CheckResult{}
	for _, check := range result.Checks {
		byID[check.ID] = check
	}
	for _, id := range []string{"api_live", "api_ready", "api_smoke", "ticket_api"} {
		check, ok := byID[id]
		if !ok {
			t.Fatalf("missing check %s", id)
		}
		if check.Status != CheckStatusOK {
			t.Fatalf("check %s expected ok, got %s: %s", id, check.Status, check.Message)
		}
	}
	if byID["tgifts_grpc"].Status != CheckStatusSkip {
		t.Fatalf("tgifts should be skipped")
	}
}

func TestMonitorServiceDetectsReadyFailure(t *testing.T) {
	server := httptest.NewServer(http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/health/ready" {
			w.WriteHeader(http.StatusServiceUnavailable)
			return
		}
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte(`ok`))
	}))
	defer server.Close()

	repo := newMemoryMonitorRepo()
	alerts := NewAlertService(repo, time.Minute)
	svc := NewService(
		Config{
			Enabled:             true,
			LooksRatingHTTPBase: server.URL,
			LooksRatingAPIKey:   "k",
			TicketAPIHealthURL:  server.URL + "/health/live",
			TgiftsEnabled:       false,
		},
		repo,
		nil,
		alerts,
		NewLogTailService(LogTailConfig{Enabled: false}, nil),
		nil,
		time.Second,
	)

	result, err := svc.Run(context.Background())
	if err != nil {
		t.Fatalf("run: %v", err)
	}
	if result.OverallStatus != OverallStatusFail {
		t.Fatal("expected failing overall status")
	}

	alert, err := repo.GetAlertByFingerprint(context.Background(), "api_ready:down")
	if err != nil || alert == nil || alert.Status != domain.MonitorAlertStatusOpen {
		t.Fatalf("expected open alert for api_ready, got %#v err=%v", alert, err)
	}
}
