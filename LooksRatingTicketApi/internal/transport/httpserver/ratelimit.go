package httpserver

import (
	"sync"
	"time"

	"looksrating/ticketapi/internal/domain"
)

type authRateLimiter struct {
	mu       sync.Mutex
	limit    int
	window   time.Duration
	attempts map[int64][]time.Time
}

func newAuthRateLimiter(limit int, window time.Duration) *authRateLimiter {
	return &authRateLimiter{
		limit:    limit,
		window:   window,
		attempts: make(map[int64][]time.Time),
	}
}

func (l *authRateLimiter) allow(telegramID int64) error {
	now := time.Now()

	l.mu.Lock()
	defer l.mu.Unlock()

	history := l.attempts[telegramID]
	cutoff := now.Add(-l.window)
	alive := history[:0]
	for _, ts := range history {
		if ts.After(cutoff) {
			alive = append(alive, ts)
		}
	}

	if len(alive) >= l.limit {
		l.attempts[telegramID] = alive
		return domain.ErrTooManyRequests
	}

	l.attempts[telegramID] = append(alive, now)
	return nil
}
