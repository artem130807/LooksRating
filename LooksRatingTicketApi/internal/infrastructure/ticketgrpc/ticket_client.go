package ticketgrpc

import (
	"context"
	"fmt"
	"strings"
	"time"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/metadata"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
	"looksrating/ticketapi/internal/gen/ticketspb"
)

const maxTicketPageSize = 500

type TicketClient struct {
	conn    *grpc.ClientConn
	client  ticketspb.AdminTicketServiceClient
	apiKey  string
	timeout time.Duration
}

func NewTicketClient(address, apiKey string, timeout time.Duration) (*TicketClient, error) {
	if timeout <= 0 {
		timeout = 15 * time.Second
	}

	conn, err := grpc.NewClient(
		address,
		grpc.WithTransportCredentials(insecure.NewCredentials()),
	)
	if err != nil {
		return nil, fmt.Errorf("grpc dial: %w", err)
	}

	return &TicketClient{
		conn:    conn,
		client:  ticketspb.NewAdminTicketServiceClient(conn),
		apiKey:  strings.TrimSpace(apiKey),
		timeout: timeout,
	}, nil
}

func (c *TicketClient) Close() error {
	if c.conn == nil {
		return nil
	}
	return c.conn.Close()
}

func (c *TicketClient) Ping(ctx context.Context) error {
	_, err := c.ListModerationCities(ctx)
	return err
}

func (c *TicketClient) ListModerationCities(ctx context.Context) ([]string, error) {
	callCtx, cancel := c.callContext(ctx)
	defer cancel()

	resp, err := c.client.ListModerationCities(callCtx, &ticketspb.ListModerationCitiesRequest{})
	if err != nil {
		return nil, mapGRPCError(err)
	}
	return resp.GetCities(), nil
}

func (c *TicketClient) ListTicketsByCity(ctx context.Context, city string, offset, limit int) (*contracts.TicketPage, error) {
	if limit <= 0 {
		limit = 1
	}
	if limit > maxTicketPageSize {
		limit = maxTicketPageSize
	}
	if offset < 0 {
		offset = 0
	}

	callCtx, cancel := c.callContext(ctx)
	defer cancel()

	resp, err := c.client.ListTicketsByCity(callCtx, &ticketspb.ListTicketsByCityRequest{
		City:   city,
		Offset: int32(offset),
		Limit:  int32(limit),
	})
	if err != nil {
		return nil, mapGRPCError(err)
	}

	ids := make([]string, 0, len(resp.GetTickets()))
	for _, ticket := range resp.GetTickets() {
		ids = append(ids, ticket.GetTicketId())
	}

	return &contracts.TicketPage{
		TicketIDs:  ids,
		TotalCount: int(resp.GetTotalCount()),
	}, nil
}

func (c *TicketClient) CountTicketsByCity(ctx context.Context, city string) (string, int, error) {
	page, err := c.ListTicketsByCity(ctx, city, 0, 1)
	if err != nil {
		return "", 0, err
	}
	return city, page.TotalCount, nil
}

func (c *TicketClient) GetQueuedTicket(ctx context.Context, city string, offset int) (*contracts.QueuedTicket, error) {
	page, err := c.ListTicketsByCity(ctx, city, offset, 1)
	if err != nil {
		return nil, err
	}

	queued := &contracts.QueuedTicket{
		ResolvedCity: city,
		TotalCount:   page.TotalCount,
		Offset:       offset,
	}
	if len(page.TicketIDs) == 0 {
		return queued, nil
	}

	detail, err := c.GetTicketDetail(ctx, page.TicketIDs[0])
	if err != nil {
		return nil, err
	}
	queued.Ticket = detail
	return queued, nil
}

func (c *TicketClient) GetTicketDetail(ctx context.Context, ticketID string) (*contracts.TicketDetail, error) {
	callCtx, cancel := c.callContext(ctx)
	defer cancel()

	resp, err := c.client.GetTicketDetail(callCtx, &ticketspb.GetTicketDetailRequest{
		TicketId: ticketID,
	})
	if err != nil {
		return nil, mapGRPCError(err)
	}

	photos := make([]contracts.TicketPhoto, 0, len(resp.GetPhotos()))
	for _, photo := range resp.GetPhotos() {
		photos = append(photos, contracts.TicketPhoto{
			ID:             photo.GetPhotoId(),
			TelegramFileID: photo.GetTelegramFileId(),
			SortOrder:      photo.GetSortOrder(),
		})
	}

	return &contracts.TicketDetail{
		ID:                  resp.GetTicketId(),
		Description:         resp.GetDescription(),
		ReporterTelegramID:  resp.GetReporterTelegramId(),
		ReporterDisplayName: resp.GetReporterDisplayName(),
		ReporterCity:        resp.GetReporterCity(),
		PhotoProfileID:      resp.GetPhotoProfileId(),
		ProfileDisplayName:  resp.GetProfileDisplayName(),
		ProfileCity:         resp.GetProfileCity(),
		ProfileAge:          resp.GetProfileAge(),
		ProfileGender:       resp.GetProfileGender(),
		ProfileRating:       resp.GetProfileRating(),
		ProfileRatingCount:  resp.GetProfileRatingCount(),
		ProfileRank:         resp.GetProfileRank(),
		Photos:              photos,
	}, nil
}

func (c *TicketClient) callContext(ctx context.Context) (context.Context, context.CancelFunc) {
	callCtx, cancel := context.WithTimeout(ctx, c.timeout)
	if c.apiKey != "" {
		callCtx = metadata.AppendToOutgoingContext(callCtx, "x-api-key", c.apiKey)
	}
	return callCtx, cancel
}
