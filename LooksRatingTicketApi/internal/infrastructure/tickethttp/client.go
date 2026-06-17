package tickethttp

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"net/url"
	"strings"
	"time"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

const maxTicketPageSize = 500

type Client struct {
	baseURL    string
	apiKey     string
	httpClient *http.Client
}

func NewClient(baseURL, apiKey string, timeout time.Duration) *Client {
	if timeout <= 0 {
		timeout = 15 * time.Second
	}

	return &Client{
		baseURL: strings.TrimRight(strings.TrimSpace(baseURL), "/"),
		apiKey:  strings.TrimSpace(apiKey),
		httpClient: &http.Client{
			Timeout: timeout,
		},
	}
}

func (c *Client) Ping(ctx context.Context) error {
	_, err := c.ListModerationCities(ctx)
	return err
}

func (c *Client) ListModerationCities(ctx context.Context) ([]string, error) {
	var response map[string]json.RawMessage
	if err := c.getJSON(ctx, "/api/internal/moderation/cities", &response); err != nil {
		return nil, err
	}
	return decodeStringSlice(response, "cities", "Cities"), nil
}

func (c *Client) CountTicketsByCity(ctx context.Context, city string) (string, int, error) {
	path := fmt.Sprintf(
		"/api/internal/moderation/tickets-by-city/count?city=%s",
		url.QueryEscape(city),
	)

	var response map[string]json.RawMessage
	if err := c.getJSON(ctx, path, &response); err != nil {
		return "", 0, err
	}

	resolvedCity := decodeString(response, "resolvedCity", "ResolvedCity")
	totalCount := decodeInt(response, "totalCount", "TotalCount")
	return resolvedCity, totalCount, nil
}

func (c *Client) GetQueuedTicket(ctx context.Context, city string, offset int) (*contracts.QueuedTicket, error) {
	if offset < 0 {
		offset = 0
	}

	path := fmt.Sprintf(
		"/api/internal/moderation/tickets-by-city/queue?city=%s&offset=%d",
		url.QueryEscape(city),
		offset,
	)

	var response map[string]json.RawMessage
	if err := c.getJSON(ctx, path, &response); err != nil {
		return nil, err
	}

	queued := &contracts.QueuedTicket{
		ResolvedCity: decodeString(response, "resolvedCity", "ResolvedCity"),
		TotalCount: decodeInt(response, "totalCount", "TotalCount"),
		Offset:     decodeInt(response, "offset", "Offset"),
	}

	if rawTicket, ok := response["ticket"]; ok && len(rawTicket) > 0 && string(rawTicket) != "null" {
		var ticketPayload map[string]json.RawMessage
		if err := json.Unmarshal(rawTicket, &ticketPayload); err == nil {
			queued.Ticket = decodeTicketDetail(ticketPayload)
		}
	}

	return queued, nil
}

func (c *Client) ListTicketsByCity(ctx context.Context, city string, offset, limit int) (*contracts.TicketPage, error) {
	if limit <= 0 {
		limit = 1
	}
	if limit > maxTicketPageSize {
		limit = maxTicketPageSize
	}
	if offset < 0 {
		offset = 0
	}

	path := fmt.Sprintf(
		"/api/internal/moderation/tickets-by-city?city=%s&offset=%d&limit=%d",
		url.QueryEscape(city),
		offset,
		limit,
	)

	var response map[string]json.RawMessage
	if err := c.getJSON(ctx, path, &response); err != nil {
		return nil, err
	}

	totalCount := decodeInt(response, "totalCount", "TotalCount")
	ids := decodeTicketIDs(response, "tickets", "Tickets")
	if len(ids) == 0 && totalCount > 0 {
		log.Printf("looksrating http warning: city=%q offset=%d returned totalCount=%d but no ticket ids", city, offset, totalCount)
	}

	return &contracts.TicketPage{
		TicketIDs:  ids,
		TotalCount: totalCount,
	}, nil
}

func (c *Client) GetTicketDetail(ctx context.Context, ticketID string) (*contracts.TicketDetail, error) {
	path := fmt.Sprintf("/api/internal/moderation/tickets/%s", url.PathEscape(ticketID))

	var response map[string]json.RawMessage
	if err := c.getJSON(ctx, path, &response); err != nil {
		return nil, err
	}

	return decodeTicketDetail(response), nil
}

func (c *Client) getJSON(ctx context.Context, path string, dst any) error {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, c.baseURL+path, nil)
	if err != nil {
		return err
	}
	return c.doJSON(req, dst)
}

func (c *Client) doJSON(req *http.Request, dst any) error {
	if c.apiKey != "" {
		req.Header.Set("X-Api-Key", c.apiKey)
	}

	resp, err := c.httpClient.Do(req)
	if err != nil {
		return domain.ErrUpstreamUnavailable
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(io.LimitReader(resp.Body, 1<<20))
	if err != nil {
		return domain.ErrUpstreamUnavailable
	}

	if resp.StatusCode >= 400 {
		log.Printf("looksrating http error: status=%d body=%s", resp.StatusCode, strings.TrimSpace(string(body)))
		return mapHTTPError(resp.StatusCode, body)
	}

	if dst == nil || len(body) == 0 {
		return nil
	}

	if err := json.Unmarshal(body, dst); err != nil {
		return fmt.Errorf("decode response: %w", err)
	}

	return nil
}

func mapHTTPError(status int, body []byte) error {
	message := strings.TrimSpace(string(body))
	var payload struct {
		Error string `json:"error"`
	}
	if err := json.Unmarshal(body, &payload); err == nil && payload.Error != "" {
		message = payload.Error
	}

	switch status {
	case http.StatusBadRequest:
		return domain.ErrInvalidRequest
	case http.StatusUnauthorized, http.StatusForbidden:
		if message != "" {
			return fmt.Errorf("доступ к LooksRating API отклонён: %s", message)
		}
		return domain.ErrAdminNotAuthenticated
	case http.StatusInternalServerError, http.StatusNotImplemented:
		if message != "" {
			return fmt.Errorf("%s", message)
		}
		return domain.ErrUpstreamUnavailable
	case http.StatusNotFound:
		if strings.Contains(message, "жалоб") {
			return domain.ErrTicketNotFound
		}
		return domain.ErrTicketNotFound
	case http.StatusTooManyRequests:
		return domain.ErrTooManyRequests
	case http.StatusBadGateway, http.StatusServiceUnavailable, http.StatusGatewayTimeout:
		return domain.ErrUpstreamUnavailable
	default:
		if message != "" {
			return fmt.Errorf("%s", message)
		}
		return domain.ErrUpstreamUnavailable
	}
}
