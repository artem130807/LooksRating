package httpserver

import (
	"log"
	"net/http"
	"time"
)

func NewServer(apiKey string, handlers *Handlers) http.Handler {
	mux := http.NewServeMux()

	mux.HandleFunc("GET /health", handlers.Health)

	mux.HandleFunc("POST /api/sessions/ensure", handlers.EnsureSession)
	mux.HandleFunc("GET /api/sessions", handlers.GetSession)
	mux.HandleFunc("POST /api/sessions/begin-login", handlers.BeginLogin)
	mux.HandleFunc("POST /api/sessions/submit-login", handlers.SubmitLogin)
	mux.HandleFunc("POST /api/sessions/authenticate", handlers.Authenticate)
	mux.HandleFunc("POST /api/sessions/logout", handlers.Logout)

	mux.HandleFunc("GET /api/moderation/cities", handlers.ListCities)
	mux.HandleFunc("POST /api/moderation/select-city", handlers.SelectCity)
	mux.HandleFunc("GET /api/moderation/current", handlers.CurrentTicket)
	mux.HandleFunc("POST /api/moderation/skip", handlers.SkipCurrent)
	mux.HandleFunc("POST /api/moderation/dismiss", handlers.DismissCurrent)
	mux.HandleFunc("POST /api/moderation/delete", handlers.DeleteCurrent)
	mux.HandleFunc("POST /api/moderation/delete-account", handlers.DeleteCurrentAccount)

	mux.HandleFunc("GET /api/monitoring/status", handlers.MonitoringStatus)
	mux.HandleFunc("POST /api/monitoring/run", handlers.MonitoringRun)
	mux.HandleFunc("GET /api/monitoring/alerts", handlers.MonitoringAlerts)
	mux.HandleFunc("GET /api/monitoring/alerts/pending", handlers.MonitoringAlertsPending)
	mux.HandleFunc("POST /api/monitoring/alerts/{id}/ack", handlers.MonitoringAlertAck)
	mux.HandleFunc("GET /api/monitoring/logs", handlers.MonitoringLogs)
	mux.HandleFunc("GET /api/admins/alert-recipients", handlers.AlertRecipients)

	return recoveryMiddleware(timingMiddleware(apiKeyMiddleware(apiKey, mux)))
}

func timingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/health" {
			next.ServeHTTP(w, r)
			return
		}

		started := time.Now()
		next.ServeHTTP(w, r)
		elapsed := time.Since(started)
		if elapsed >= 500*time.Millisecond {
			log.Printf("slow request: %s %s took %s", r.Method, r.URL.Path, elapsed.Round(time.Millisecond))
		}
	})
}
