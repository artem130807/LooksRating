package ticketgrpc

import (
	"errors"
	"log"

	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"

	"looksrating/ticketapi/internal/domain"
)

func mapGRPCError(err error) error {
	if err == nil {
		return nil
	}
	st, ok := status.FromError(err)
	if !ok {
		return err
	}

	switch st.Code() {
	case codes.NotFound:
		return domain.ErrTicketNotFound
	case codes.InvalidArgument:
		return domain.ErrInvalidRequest
	case codes.Unauthenticated, codes.PermissionDenied:
		return domain.ErrAdminNotAuthenticated
	case codes.Unavailable, codes.DeadlineExceeded:
		return domain.ErrUpstreamUnavailable
	case codes.ResourceExhausted:
		return domain.ErrTooManyRequests
	case codes.Unimplemented:
		return domain.ErrUpstreamMisconfigured
	default:
		log.Printf("grpc upstream error: code=%s message=%s", st.Code(), st.Message())
		return domain.ErrUpstreamUnavailable
	}
}

func IsNotFound(err error) bool {
	return errors.Is(err, domain.ErrTicketNotFound)
}
