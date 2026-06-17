package contracts

import "context"

type TicketPhoto struct {
	ID             string
	TelegramFileID string
	SortOrder      int32
}

type TicketDetail struct {
	ID                  string
	Description         string
	ReporterTelegramID  int64
	ReporterDisplayName string
	ReporterCity        string
	PhotoProfileID      string
	ProfileTelegramID   int64
	ProfileDisplayName  string
	ProfileCity         string
	ProfileAge          int32
	ProfileGender       string
	ProfileRating       float64
	ProfileRatingCount  int32
	ProfileRank         string
	Photos              []TicketPhoto
}

type TicketPage struct {
	TicketIDs  []string
	TotalCount int
}

type QueuedTicket struct {
	ResolvedCity string
	TotalCount   int
	Offset       int
	Ticket       *TicketDetail
}

type TicketClient interface {
	ListModerationCities(ctx context.Context) ([]string, error)
	ListTicketsByCity(ctx context.Context, city string, offset, limit int) (*TicketPage, error)
	CountTicketsByCity(ctx context.Context, city string) (string, int, error)
	GetQueuedTicket(ctx context.Context, city string, offset int) (*QueuedTicket, error)
	GetTicketDetail(ctx context.Context, ticketID string) (*TicketDetail, error)
}
