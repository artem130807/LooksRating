package contracts

import "context"

type ModerationActionResult struct {
	IsSuccess bool
	Message   string
}

type ModerationActionsClient interface {
	RejectTicketPhotoProfile(ctx context.Context, ticketID string) (*ModerationActionResult, error)
	RemoveTicketsPhotoprofile(ctx context.Context, photoProfileID string) (*ModerationActionResult, error)
}
