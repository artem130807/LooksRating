package moderation

import "looksrating/ticketapi/internal/domain/contracts"

type CurrentTicketView struct {
	City      string
	Remaining int
	Ticket    *contracts.TicketDetail
}
