package httpserver

import (
	"encoding/json"
	"io"
	"net/http"
	"strconv"
	"time"

	"looksrating/ticketapi/internal/application/moderation"
	"looksrating/ticketapi/internal/application/monitoring"
	"looksrating/ticketapi/internal/application/session"
)

type Handlers struct {
	sessions    *session.Service
	moderation  *moderation.Service
	monitoring  *monitoring.Service
	authLimiter *authRateLimiter
}

func NewHandlers(
	sessions *session.Service,
	moderation *moderation.Service,
	monitoringService *monitoring.Service,
) *Handlers {
	return &Handlers{
		sessions:    sessions,
		moderation:  moderation,
		monitoring:  monitoringService,
		authLimiter: newAuthRateLimiter(8, time.Minute),
	}
}

func (h *Handlers) Health(w http.ResponseWriter, _ *http.Request) {
	writeJSON(w, http.StatusOK, map[string]string{"status": "ok"})
}

func (h *Handlers) EnsureSession(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	userSession, err := h.sessions.Ensure(r.Context(), req.TelegramID)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, mapSession(userSession))
}

func (h *Handlers) GetSession(w http.ResponseWriter, r *http.Request) {
	telegramID, err := telegramIDFromQuery(r)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: err.Error()})
		return
	}

	userSession, err := h.sessions.Get(r.Context(), telegramID)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, mapSession(userSession))
}

func (h *Handlers) SubmitLogin(w http.ResponseWriter, r *http.Request) {
	var req submitLoginRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	userSession, err := h.sessions.SubmitLogin(r.Context(), req.TelegramID, req.Login)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, mapSession(userSession))
}

func (h *Handlers) BeginLogin(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	userSession, err := h.sessions.BeginLogin(r.Context(), req.TelegramID)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, mapSession(userSession))
}

func (h *Handlers) Authenticate(w http.ResponseWriter, r *http.Request) {
	var req authenticateRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	if err := h.authLimiter.allow(req.TelegramID); err != nil {
		writeError(w, err)
		return
	}

	userSession, err := h.sessions.Authenticate(r.Context(), req.TelegramID, req.Login, req.Password)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, mapSession(userSession))
}

func (h *Handlers) Logout(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	if err := h.sessions.Logout(r.Context(), req.TelegramID); err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"success": true})
}

func (h *Handlers) ListCities(w http.ResponseWriter, r *http.Request) {
	telegramID, err := telegramIDFromQuery(r)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: err.Error()})
		return
	}

	cities, err := h.moderation.BeginCitySelection(r.Context(), telegramID)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, citiesResponse{Cities: cities})
}

func (h *Handlers) SelectCity(w http.ResponseWriter, r *http.Request) {
	var req selectCityRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	count, err := h.moderation.SelectCity(r.Context(), req.TelegramID, req.City)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, selectCityResponse{City: req.City, Count: count})
}

func (h *Handlers) CurrentTicket(w http.ResponseWriter, r *http.Request) {
	telegramID, err := telegramIDFromQuery(r)
	if err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: err.Error()})
		return
	}

	view, err := h.moderation.GetCurrentView(r.Context(), telegramID)
	if err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, currentTicketResponse{
		City:      view.City,
		Remaining: view.Remaining,
		Ticket:    mapTicket(view.Ticket),
	})
}

func (h *Handlers) SkipCurrent(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	if err := h.moderation.SkipCurrent(r.Context(), req.TelegramID); err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"success": true})
}

func (h *Handlers) DismissCurrent(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	if err := h.moderation.DismissCurrent(r.Context(), req.TelegramID); err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"success": true})
}

func (h *Handlers) DeleteCurrent(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	if err := h.moderation.DeleteCurrentProfile(r.Context(), req.TelegramID); err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"success": true})
}

func (h *Handlers) DeleteCurrentAccount(w http.ResponseWriter, r *http.Request) {
	var req telegramRequest
	if err := decodeJSON(r, &req); err != nil {
		writeJSON(w, http.StatusBadRequest, errorResponse{Error: "некорректное тело запроса"})
		return
	}

	if err := h.moderation.DeleteCurrentUserAccount(r.Context(), req.TelegramID); err != nil {
		writeError(w, err)
		return
	}

	writeJSON(w, http.StatusOK, map[string]bool{"success": true})
}

func decodeJSON(r *http.Request, dst any) error {
	decoder := json.NewDecoder(io.LimitReader(r.Body, 1<<20))
	decoder.DisallowUnknownFields()
	return decoder.Decode(dst)
}

func telegramIDFromQuery(r *http.Request) (int64, error) {
	raw := r.URL.Query().Get("telegramId")
	if raw == "" {
		return 0, errTelegramIDRequired()
	}
	telegramID, err := strconv.ParseInt(raw, 10, 64)
	if err != nil || telegramID <= 0 {
		return 0, errTelegramIDRequired()
	}
	return telegramID, nil
}

func errTelegramIDRequired() error {
	return &badRequestError{message: "telegramId обязателен"}
}

type badRequestError struct {
	message string
}

func (e *badRequestError) Error() string {
	return e.message
}
