package httpserver

import (
	"net/http"
	"strconv"
	"strings"

	"looksrating/ticketapi/internal/application/monitoring"
)

func (h *Handlers) MonitoringStatus(w http.ResponseWriter, r *http.Request) {
	telegramID, err := telegramIDFromQuery(r)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: err.Error()})
		return
	}
	if err := h.sessions.RequireAuthenticated(r.Context(), telegramID); err != nil {
		writeError(w, err)
		return
	}
	if h.monitoring == nil {
		writeJSON(w, http.StatusServiceUnavailable, errorResponse{Error: "мониторинг отключён"})
		return
	}

	status, err := h.monitoring.GetStatus(r.Context())
	if err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, status)
}

func (h *Handlers) MonitoringRun(w http.ResponseWriter, r *http.Request) {
	var telegramID int64
	if raw := r.URL.Query().Get("telegramId"); raw != "" {
		parsed, err := strconv.ParseInt(raw, 10, 64)
		if err != nil || parsed <= 0 {
			writeJSON(w, http.StatusBadRequest, errorResponse{Error: "telegramId обязателен"})
			return
		}
		telegramID = parsed
	} else {
		var req telegramRequest
		if err := decodeJSON(r, &req); err != nil || req.TelegramID <= 0 {
			writeJSON(w, http.StatusBadRequest, errorResponse{Error: "telegramId обязателен"})
			return
		}
		telegramID = req.TelegramID
	}
	if err := h.sessions.RequireAuthenticated(r.Context(), telegramID); err != nil {
		writeError(w, err)
		return
	}
	if h.monitoring == nil {
		writeJSON(w, http.StatusServiceUnavailable, errorResponse{Error: "мониторинг отключён"})
		return
	}

	result, err := h.monitoring.Run(r.Context())
	if err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, result)
}

func (h *Handlers) MonitoringAlerts(w http.ResponseWriter, r *http.Request) {
	telegramID, err := telegramIDFromQuery(r)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: err.Error()})
		return
	}
	if err := h.sessions.RequireAuthenticated(r.Context(), telegramID); err != nil {
		writeError(w, err)
		return
	}
	if h.monitoring == nil {
		writeJSON(w, http.StatusServiceUnavailable, errorResponse{Error: "мониторинг отключён"})
		return
	}

	alerts, err := h.monitoring.ListOpenAlerts(r.Context())
	if err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"alerts": alerts})
}

func (h *Handlers) MonitoringAlertsPending(w http.ResponseWriter, r *http.Request) {
	if h.monitoring == nil {
		writeJSON(w, http.StatusOK, monitoring.PendingAlertsResponse{})
		return
	}

	pending, err := h.monitoring.ListPendingAlerts(r.Context())
	if err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, pending)
}

func (h *Handlers) MonitoringAlertAck(w http.ResponseWriter, r *http.Request) {
	idRaw := strings.TrimPrefix(r.PathValue("id"), "")
	alertID, err := strconv.ParseUint(idRaw, 10, 64)
	if err != nil || alertID == 0 {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректный id алерта"})
		return
	}
	if h.monitoring == nil {
		writeJSON(w, http.StatusServiceUnavailable, errorResponse{Error: "мониторинг отключён"})
		return
	}
	if err := h.monitoring.AckAlert(r.Context(), uint(alertID)); err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]bool{"success": true})
}

func (h *Handlers) MonitoringLogs(w http.ResponseWriter, r *http.Request) {
	telegramID, err := telegramIDFromQuery(r)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: err.Error()})
		return
	}
	if err := h.sessions.RequireAuthenticated(r.Context(), telegramID); err != nil {
		writeError(w, err)
		return
	}
	if h.monitoring == nil {
		writeJSON(w, http.StatusServiceUnavailable, errorResponse{Error: "мониторинг отключён"})
		return
	}

	service := strings.TrimSpace(r.URL.Query().Get("service"))
	if service == "" {
		service = "api"
	}
	tail := 80
	if raw := r.URL.Query().Get("tail"); raw != "" {
		if parsed, parseErr := strconv.Atoi(raw); parseErr == nil && parsed > 0 {
			tail = parsed
		}
	}

	logs, err := h.monitoring.TailLogs(r.Context(), service, tail)
	if err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, logs)
}

func (h *Handlers) AlertRecipients(w http.ResponseWriter, r *http.Request) {
	if h.monitoring == nil {
		writeJSON(w, http.StatusOK, map[string]any{"recipients": []int64{}})
		return
	}
	pending, err := h.monitoring.ListPendingAlerts(r.Context())
	if err != nil {
		writeError(w, err)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"recipients": pending.Recipients})
}
