package httpserver

import (
	"encoding/json"
	"errors"
	"net/http"

	"looksrating/ticketapi/internal/application/monitoring"
	"looksrating/ticketapi/internal/domain"
)

type errorResponse struct {
	Error string `json:"error"`
}

func writeJSON(w http.ResponseWriter, status int, payload any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(payload)
}

func writeError(w http.ResponseWriter, err error) {
	status := http.StatusInternalServerError
	message := "внутренняя ошибка сервера"

	var badReq *badRequestError
	var moderationErr *domain.ModerationActionError
	switch {
	case errors.As(err, &moderationErr):
		status = http.StatusBadRequest
		message = moderationErr.Error()
	case errors.Is(err, monitoring.ErrMonitoringDisabled):
		status = http.StatusServiceUnavailable
		message = err.Error()
	case errors.As(err, &badReq),
		errors.Is(err, domain.ErrInvalidTelegramID),
		errors.Is(err, domain.ErrInvalidState),
		errors.Is(err, domain.ErrInvalidPassword),
		errors.Is(err, domain.ErrInvalidRequest):
		status = http.StatusBadRequest
		message = err.Error()
	case errors.Is(err, domain.ErrSessionNotFound),
		errors.Is(err, domain.ErrAdminNotFound),
		errors.Is(err, domain.ErrTicketQueueEmpty),
		errors.Is(err, domain.ErrTicketNotFound),
		errors.Is(err, monitoring.ErrAlertNotFound):
		status = http.StatusNotFound
		message = err.Error()
	case errors.Is(err, domain.ErrAdminNotAuthenticated):
		status = http.StatusUnauthorized
		message = err.Error()
	case errors.Is(err, domain.ErrUpstreamUnavailable),
		errors.Is(err, domain.ErrUpstreamMisconfigured):
		status = http.StatusBadGateway
		message = err.Error()
	case errors.Is(err, domain.ErrTooManyRequests):
		status = http.StatusTooManyRequests
		message = err.Error()
	}

	writeJSON(w, status, errorResponse{Error: message})
}
