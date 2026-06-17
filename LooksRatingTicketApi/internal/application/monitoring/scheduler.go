package monitoring

import (
	"context"
	"log"
	"time"
)

type Scheduler struct {
	service  *Service
	interval time.Duration
	stop     chan struct{}
	done     chan struct{}
}

func NewScheduler(service *Service, interval time.Duration) *Scheduler {
	if interval <= 0 {
		interval = time.Minute
	}
	return &Scheduler{
		service:  service,
		interval: interval,
		stop:     make(chan struct{}),
		done:     make(chan struct{}),
	}
}

func (s *Scheduler) Start() {
	if s.service == nil || !s.service.Enabled() {
		log.Println("monitoring scheduler disabled")
		close(s.done)
		return
	}

	go func() {
		defer close(s.done)
		ticker := time.NewTicker(s.interval)
		defer ticker.Stop()

		s.runOnce()
		for {
			select {
			case <-ticker.C:
				s.runOnce()
			case <-s.stop:
				return
			}
		}
	}()
}

func (s *Scheduler) Stop(ctx context.Context) {
	close(s.stop)
	select {
	case <-s.done:
	case <-ctx.Done():
	}
}

func (s *Scheduler) runOnce() {
	ctx, cancel := context.WithTimeout(context.Background(), 45*time.Second)
	defer cancel()
	if _, err := s.service.Run(ctx); err != nil {
		log.Printf("monitor run failed: %v", err)
	}
}
