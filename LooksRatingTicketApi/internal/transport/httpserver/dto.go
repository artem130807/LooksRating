package httpserver

import (
	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

type telegramRequest struct {
	TelegramID int64 `json:"telegramId"`
}

type authenticateRequest struct {
	TelegramID int64  `json:"telegramId"`
	Login      string `json:"login"`
	Password   string `json:"password"`
}

type submitLoginRequest struct {
	TelegramID int64  `json:"telegramId"`
	Login      string `json:"login"`
}

type selectCityRequest struct {
	TelegramID int64  `json:"telegramId"`
	City       string `json:"city"`
}

type sessionResponse struct {
	TelegramID int64             `json:"telegramId"`
	State      string             `json:"state"`
	IsAuth     bool               `json:"isAuthenticated"`
	Payload    map[string]string  `json:"payload,omitempty"`
}

type citiesResponse struct {
	Cities []string `json:"cities"`
}

type selectCityResponse struct {
	City  string `json:"city"`
	Count int    `json:"count"`
}

type ticketPhotoResponse struct {
	ID             string `json:"id"`
	TelegramFileID string `json:"telegramFileId"`
	SortOrder      int32  `json:"sortOrder"`
}

type ticketDetailResponse struct {
	ID                  string                `json:"id"`
	Description         string                `json:"description"`
	ReporterTelegramID  int64                 `json:"reporterTelegramId"`
	ReporterDisplayName string                `json:"reporterDisplayName"`
	ReporterCity        string                `json:"reporterCity"`
	PhotoProfileID      string                `json:"photoProfileId"`
	ProfileTelegramID   int64                 `json:"profileTelegramId"`
	ProfileDisplayName  string                `json:"profileDisplayName"`
	ProfileCity         string                `json:"profileCity"`
	ProfileAge          int32                 `json:"profileAge"`
	ProfileGender       string                `json:"profileGender"`
	ProfileRating       float64               `json:"profileRating"`
	ProfileRatingCount  int32                 `json:"profileRatingCount"`
	ProfileRank         string                `json:"profileRank"`
	Photos              []ticketPhotoResponse `json:"photos"`
}

type currentTicketResponse struct {
	City      string               `json:"city"`
	Remaining int                  `json:"remaining"`
	Ticket    ticketDetailResponse `json:"ticket"`
}

func mapSession(session *domain.UserSession) sessionResponse {
	payload := map[string]string{}
	for key, value := range session.Payload {
		payload[key] = value
	}
	return sessionResponse{
		TelegramID: session.TelegramID,
		State:      string(session.State),
		IsAuth:     session.IsAuthenticated(),
		Payload:    payload,
	}
}

func mapTicket(ticket *contracts.TicketDetail) ticketDetailResponse {
	photos := make([]ticketPhotoResponse, 0, len(ticket.Photos))
	for _, photo := range ticket.Photos {
		photos = append(photos, ticketPhotoResponse{
			ID:             photo.ID,
			TelegramFileID: photo.TelegramFileID,
			SortOrder:      photo.SortOrder,
		})
	}

	return ticketDetailResponse{
		ID:                  ticket.ID,
		Description:         ticket.Description,
		ReporterTelegramID:  ticket.ReporterTelegramID,
		ReporterDisplayName: ticket.ReporterDisplayName,
		ReporterCity:        ticket.ReporterCity,
		PhotoProfileID:      ticket.PhotoProfileID,
		ProfileTelegramID:   ticket.ProfileTelegramID,
		ProfileDisplayName:  ticket.ProfileDisplayName,
		ProfileCity:         ticket.ProfileCity,
		ProfileAge:          ticket.ProfileAge,
		ProfileGender:       ticket.ProfileGender,
		ProfileRating:       ticket.ProfileRating,
		ProfileRatingCount:  ticket.ProfileRatingCount,
		ProfileRank:         ticket.ProfileRank,
		Photos:              photos,
	}
}
