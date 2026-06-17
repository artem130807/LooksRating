package looksratinggrpc

import (
	"context"
	"fmt"
	"strings"
	"time"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
	"google.golang.org/grpc/encoding"
	"google.golang.org/grpc/metadata"
	"google.golang.org/protobuf/encoding/protowire"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

const (
	rejectTicketMethod  = "/RejectTicketPhotoProfileService/RejectTicketPhotoProfile"
	removeTicketsMethod = "/RemoveTicketsPhotoprofileService/RemoveTicketsPhotoprofile"
	bytesCodecName      = "looksraw"
)

func init() {
	encoding.RegisterCodec(bytesCodec{})
}

type bytesCodec struct{}

func (bytesCodec) Name() string { return bytesCodecName }

func (bytesCodec) Marshal(v any) ([]byte, error) {
	payload, ok := v.([]byte)
	if !ok {
		return nil, fmt.Errorf("bytes codec: expected []byte, got %T", v)
	}
	return payload, nil
}

func (bytesCodec) Unmarshal(data []byte, v any) error {
	target, ok := v.(*[]byte)
	if !ok {
		return fmt.Errorf("bytes codec: expected *[]byte, got %T", v)
	}
	*target = append((*target)[:0], data...)
	return nil
}

type ModerationActionsClient struct {
	conn    *grpc.ClientConn
	apiKey  string
	timeout time.Duration
}

func NewModerationActionsClient(address, apiKey string, timeout time.Duration) (*ModerationActionsClient, error) {
	address = strings.TrimSpace(address)
	if address == "" {
		return nil, fmt.Errorf("grpc address is required")
	}
	if timeout <= 0 {
		timeout = 15 * time.Second
	}

	conn, err := grpc.NewClient(
		address,
		grpc.WithTransportCredentials(insecure.NewCredentials()),
		grpc.WithDefaultCallOptions(grpc.CallContentSubtype(bytesCodecName)),
	)
	if err != nil {
		return nil, fmt.Errorf("grpc dial: %w", err)
	}

	return &ModerationActionsClient{
		conn:    conn,
		apiKey:  strings.TrimSpace(apiKey),
		timeout: timeout,
	}, nil
}

func (c *ModerationActionsClient) Close() error {
	if c.conn == nil {
		return nil
	}
	return c.conn.Close()
}

func (c *ModerationActionsClient) RejectTicketPhotoProfile(
	ctx context.Context,
	ticketID string,
) (*contracts.ModerationActionResult, error) {
	ticketID = strings.TrimSpace(ticketID)
	if ticketID == "" {
		return nil, domain.ErrInvalidRequest
	}

	response, err := c.invoke(ctx, rejectTicketMethod, marshalStringField(1, ticketID))
	if err != nil {
		return nil, err
	}
	return parseActionResponse(response)
}

func (c *ModerationActionsClient) RemoveTicketsPhotoprofile(
	ctx context.Context,
	photoProfileID string,
) (*contracts.ModerationActionResult, error) {
	photoProfileID = strings.TrimSpace(photoProfileID)
	if photoProfileID == "" {
		return nil, domain.ErrInvalidRequest
	}

	response, err := c.invoke(ctx, removeTicketsMethod, marshalStringField(1, photoProfileID))
	if err != nil {
		return nil, err
	}
	return parseActionResponse(response)
}

func (c *ModerationActionsClient) invoke(ctx context.Context, method string, payload []byte) ([]byte, error) {
	callCtx, cancel := context.WithTimeout(ctx, c.timeout)
	defer cancel()

	if c.apiKey != "" {
		callCtx = metadata.AppendToOutgoingContext(callCtx, "x-api-key", c.apiKey)
	}

	var response []byte
	err := c.conn.Invoke(callCtx, method, payload, &response)
	if err != nil {
		return nil, mapGRPCError(err)
	}
	return response, nil
}

func marshalStringField(fieldNumber protowire.Number, value string) []byte {
	var out []byte
	out = protowire.AppendTag(out, fieldNumber, protowire.BytesType)
	out = protowire.AppendString(out, value)
	return out
}

func parseActionResponse(payload []byte) (*contracts.ModerationActionResult, error) {
	result := &contracts.ModerationActionResult{}
	for len(payload) > 0 {
		num, wireType, n := protowire.ConsumeTag(payload)
		if n < 0 {
			return nil, fmt.Errorf("decode response tag: %w", protowire.ParseError(n))
		}
		payload = payload[n:]

		switch num {
		case 1:
			if wireType != protowire.VarintType {
				return nil, fmt.Errorf("unexpected wire type for is_success")
			}
			value, m := protowire.ConsumeVarint(payload)
			if m < 0 {
				return nil, fmt.Errorf("decode is_success: %w", protowire.ParseError(m))
			}
			payload = payload[m:]
			result.IsSuccess = value == 1
		case 2:
			if wireType != protowire.BytesType {
				return nil, fmt.Errorf("unexpected wire type for message")
			}
			value, m := protowire.ConsumeString(payload)
			if m < 0 {
				return nil, fmt.Errorf("decode message: %w", protowire.ParseError(m))
			}
			payload = payload[m:]
			result.Message = value
		default:
			m := protowire.ConsumeFieldValue(num, wireType, payload)
			if m < 0 {
				return nil, fmt.Errorf("skip unknown field: %w", protowire.ParseError(m))
			}
			payload = payload[m:]
		}
	}
	return result, nil
}

func mapGRPCError(err error) error {
	if err == nil {
		return nil
	}
	lower := strings.ToLower(err.Error())
	if strings.Contains(lower, "unavailable") || strings.Contains(lower, "connection refused") {
		return domain.ErrUpstreamUnavailable
	}
	return err
}
