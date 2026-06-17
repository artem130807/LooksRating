package monitoring

import (
	"context"
	"encoding/json"
	"fmt"
	"log"
	"strings"
	"sync"
	"time"

	"looksrating/ticketapi/internal/domain"
	"looksrating/ticketapi/internal/domain/contracts"
)

type Config struct {
	Enabled              bool
	Interval             time.Duration
	AlertCooldown        time.Duration
	LogTailLines         int
	LooksRatingHTTPBase  string
	LooksRatingAPIKey    string
	LooksRatingBotToken  string
	TgiftsEnabled        bool
	TgiftsGRPCAddr       string
	TicketAPIHealthURL   string
	MoscowLocation       *time.Location
}

type Service struct {
	cfg         Config
	repo        contracts.MonitorRepository
	admins      contracts.AdminRepository
	alerts      *AlertService
	prober      *HTTPProber
	logTail     *LogTailService
	docker      *DockerRuntime
	quartzRules []QuartzRule
	mu          sync.Mutex
	runMu       sync.Mutex
	lastRun     *RunResult
}

func NewService(
	cfg Config,
	repo contracts.MonitorRepository,
	admins contracts.AdminRepository,
	alerts *AlertService,
	logTail *LogTailService,
	docker *DockerRuntime,
	timeout time.Duration,
) *Service {
	return &Service{
		cfg:         cfg,
		repo:        repo,
		admins:      admins,
		alerts:      alerts,
		prober:      NewHTTPProber(timeout),
		logTail:     logTail,
		docker:      docker,
		quartzRules: defaultQuartzRules(),
	}
}

func (s *Service) Enabled() bool {
	return s.cfg.Enabled
}

func (s *Service) LastRun() *RunResult {
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.lastRun == nil {
		return nil
	}
	copy := *s.lastRun
	return &copy
}

func (s *Service) Run(ctx context.Context) (*RunResult, error) {
	if !s.cfg.Enabled {
		return nil, ErrMonitoringDisabled
	}

	s.runMu.Lock()
	defer s.runMu.Unlock()

	checks := make([]CheckResult, 0, 12)

	checks = append(checks, s.checkAPILive(ctx))
	checks = append(checks, s.checkAPIReady(ctx))
	checks = append(checks, s.checkAPISmoke(ctx))
	checks = append(checks, s.checkTicketAPI(ctx))
	checks = append(checks, s.checkMainBot(ctx))
	checks = append(checks, s.checkTgifts(ctx))
	checks = append(checks, s.evaluateQuartzRules(ctx, time.Now())...)

	overall := OverallStatusOK
	for _, check := range checks {
		if check.Status == CheckStatusFail {
			overall = OverallStatusFail
			break
		}
	}

	result := &RunResult{
		CheckedAt:     time.Now().UTC(),
		OverallStatus: overall,
		Checks:        checks,
	}

	if err := s.persistRun(ctx, result); err != nil {
		return result, err
	}

	s.mu.Lock()
	s.lastRun = result
	s.mu.Unlock()

	return result, nil
}

func (s *Service) GetStatus(ctx context.Context) (*StatusResponse, error) {
	if cached := s.LastRun(); cached != nil {
		return &StatusResponse{LastRun: cached}, nil
	}

	run, err := s.repo.GetLatestCheckRun(ctx)
	if err != nil {
		return nil, err
	}
	if run == nil {
		return &StatusResponse{}, nil
	}

	parsed, err := parseStoredRun(run)
	if err != nil {
		return nil, err
	}
	return &StatusResponse{LastRun: parsed}, nil
}

func (s *Service) TailLogs(ctx context.Context, service string, tail int) (*LogsResponse, error) {
	service = strings.TrimSpace(service)
	if service == "" {
		service = "api"
	}
	if _, ok := AllowedLogServices[service]; !ok {
		return nil, fmt.Errorf("неизвестный сервис: %s", service)
	}
	if tail <= 0 {
		tail = 80
	}
	if tail > MaxLogTailLines {
		tail = MaxLogTailLines
	}

	if s.logTail == nil || !s.logTail.Enabled() {
		return &LogsResponse{
			Service: service,
			Enabled: false,
			Lines:   "Логи через Docker недоступны. Включите MONITOR_ENABLE_DOCKER_LOGS и смонтируйте docker.sock.",
		}, nil
	}
	lines, err := s.logTail.Tail(ctx, service, tail)
	if err != nil {
		return &LogsResponse{
			Service: service,
			Enabled: true,
			Lines:   err.Error(),
		}, nil
	}
	return &LogsResponse{Service: service, Lines: lines, Enabled: true}, nil
}

func (s *Service) ListOpenAlerts(ctx context.Context) ([]AlertView, error) {
	alerts, err := s.alerts.ListOpen(ctx)
	if err != nil {
		return nil, err
	}
	out := make([]AlertView, 0, len(alerts))
	for _, alert := range alerts {
		out = append(out, MapAlertView(alert))
	}
	return out, nil
}

func (s *Service) ListPendingAlerts(ctx context.Context) (*PendingAlertsResponse, error) {
	alerts, err := s.alerts.ListPending(ctx)
	if err != nil {
		return nil, err
	}
	admins, err := s.admins.ListActiveWithTelegram(ctx)
	if err != nil {
		return nil, err
	}

	recipients := make([]int64, 0, len(admins))
	for _, admin := range admins {
		if admin.TelegramID != nil && *admin.TelegramID > 0 {
			recipients = append(recipients, *admin.TelegramID)
		}
	}

	out := make([]AlertView, 0, len(alerts))
	for _, alert := range alerts {
		out = append(out, MapAlertView(alert))
	}
	return &PendingAlertsResponse{Alerts: out, Recipients: recipients}, nil
}

func (s *Service) AckAlert(ctx context.Context, id uint) error {
	return s.alerts.Ack(ctx, id)
}

func (s *Service) persistRun(ctx context.Context, result *RunResult) error {
	payload, err := json.Marshal(result.Checks)
	if err != nil {
		return err
	}
	run := &domain.MonitorCheckRun{
		CheckedAt:     result.CheckedAt,
		OverallStatus: result.OverallStatus,
		ChecksJSON:    string(payload),
		CreatedAt:     time.Now().UTC(),
	}
	return s.repo.SaveCheckRun(ctx, run)
}

func parseStoredRun(run *domain.MonitorCheckRun) (*RunResult, error) {
	var checks []CheckResult
	if err := json.Unmarshal([]byte(run.ChecksJSON), &checks); err != nil {
		return nil, err
	}
	return &RunResult{
		CheckedAt:     run.CheckedAt,
		OverallStatus: run.OverallStatus,
		Checks:        checks,
	}, nil
}

func (s *Service) runCheck(ctx context.Context, id, name string, fn func(context.Context) (string, error)) CheckResult {
	started := time.Now()
	msg, err := fn(ctx)
	duration := time.Since(started)
	check := CheckResult{
		ID:       id,
		Name:     name,
		Duration: duration.Round(time.Millisecond).String(),
	}
	if err != nil {
		check.Status = CheckStatusFail
		check.Message = err.Error()
		fp := id + ":down"
		severity := domain.MonitorAlertSeverityCritical
		if strings.HasPrefix(id, "quartz:") {
			severity = domain.MonitorAlertSeverityWarning
		}
		if alertErr := s.alerts.Open(ctx, fp, severity, name, err.Error()); alertErr != nil {
			log.Printf("monitor alert open %s: %v", fp, alertErr)
		}
		return check
	}
	check.Status = CheckStatusOK
	check.Message = msg
	if alertErr := s.alerts.Resolve(ctx, id+":down", name); alertErr != nil {
		log.Printf("monitor alert resolve %s: %v", id+":down", alertErr)
	}
	return check
}

func (s *Service) checkAPILive(ctx context.Context) CheckResult {
	return s.runCheck(ctx, "api_live", "API live", func(ctx context.Context) (string, error) {
		url := strings.TrimRight(s.cfg.LooksRatingHTTPBase, "/") + "/health/live"
		code, body, err := s.prober.Get(ctx, url, "")
		if err != nil {
			return "", err
		}
		if code != 200 {
			return "", fmt.Errorf("HTTP %d: %s", code, body)
		}
		return "live", nil
	})
}

func (s *Service) checkAPIReady(ctx context.Context) CheckResult {
	return s.runCheck(ctx, "api_ready", "API ready", func(ctx context.Context) (string, error) {
		url := strings.TrimRight(s.cfg.LooksRatingHTTPBase, "/") + "/health/ready"
		code, body, err := s.prober.Get(ctx, url, "")
		if err != nil {
			return "", err
		}
		if code != 200 {
			return "", fmt.Errorf("HTTP %d: %s", code, body)
		}
		return "ready", nil
	})
}

func (s *Service) checkAPISmoke(ctx context.Context) CheckResult {
	return s.runCheck(ctx, "api_smoke", "API cities smoke", func(ctx context.Context) (string, error) {
		url := strings.TrimRight(s.cfg.LooksRatingHTTPBase, "/") + "/api/cities"
		code, body, err := s.prober.Get(ctx, url, s.cfg.LooksRatingAPIKey)
		if err != nil {
			return "", err
		}
		if code != 200 {
			return "", fmt.Errorf("HTTP %d: %s", code, body)
		}
		if !strings.Contains(body, "[") && !strings.Contains(strings.ToLower(body), "city") {
			return "", fmt.Errorf("неожиданный ответ: %s", truncate(body, 120))
		}
		return "ok", nil
	})
}

func (s *Service) checkTicketAPI(ctx context.Context) CheckResult {
	return s.runCheck(ctx, "ticket_api", "Ticket API", func(ctx context.Context) (string, error) {
		url := s.cfg.TicketAPIHealthURL
		if url == "" {
			url = "http://127.0.0.1:8090/health"
		}
		code, body, err := s.prober.Get(ctx, url, "")
		if err != nil {
			return "", err
		}
		if code != 200 {
			return "", fmt.Errorf("HTTP %d: %s", code, body)
		}
		return "ok", nil
	})
}

func (s *Service) checkMainBot(ctx context.Context) CheckResult {
	if strings.TrimSpace(s.cfg.LooksRatingBotToken) == "" {
		return CheckResult{
			ID:       "main_bot",
			Name:     "Main bot getMe",
			Status:   CheckStatusSkip,
			Message:  "LOOKS_RATING_BOT_TOKEN не задан",
			Duration: "0s",
		}
	}
	return s.runCheck(ctx, "main_bot", "Main bot getMe", func(ctx context.Context) (string, error) {
		if err := s.prober.TelegramGetMe(ctx, s.cfg.LooksRatingBotToken); err != nil {
			return "", err
		}
		return "getMe ok", nil
	})
}

func (s *Service) checkTgifts(ctx context.Context) CheckResult {
	started := time.Now()
	if !s.cfg.TgiftsEnabled {
		return CheckResult{
			ID:       "tgifts_grpc",
			Name:     "TGifts gRPC",
			Status:   CheckStatusSkip,
			Message:  "отключено (MONITOR_TGIFTS_ENABLED=false)",
			Duration: "0s",
		}
	}

	if s.docker != nil {
		state, containerName, err := s.docker.ServiceContainerState(ctx, "tgifts-buyer")
		if err != nil {
			return CheckResult{
				ID:       "tgifts_grpc",
				Name:     "TGifts gRPC",
				Status:   CheckStatusFail,
				Message:  err.Error(),
				Duration: time.Since(started).Round(time.Millisecond).String(),
			}
		}
		switch state {
		case containerNotFound:
			return CheckResult{
				ID:       "tgifts_grpc",
				Name:     "TGifts gRPC",
				Status:   CheckStatusSkip,
				Message:  "контейнер не запущен — опционально: docker compose up -d tgifts-buyer",
				Duration: time.Since(started).Round(time.Millisecond).String(),
			}
		case containerStopped:
			msg := fmt.Sprintf("контейнер %s остановлен", containerName)
			_ = s.alerts.Open(ctx, "tgifts_grpc:down", domain.MonitorAlertSeverityWarning, "TGifts gRPC", msg)
			return CheckResult{
				ID:       "tgifts_grpc",
				Name:     "TGifts gRPC",
				Status:   CheckStatusFail,
				Message:  msg,
				Duration: time.Since(started).Round(time.Millisecond).String(),
			}
		}
	}

	return s.runCheck(ctx, "tgifts_grpc", "TGifts gRPC", func(ctx context.Context) (string, error) {
		if err := TCPDial(ctx, s.cfg.TgiftsGRPCAddr, 5*time.Second); err != nil {
			return "", err
		}
		return "tcp ok", nil
	})
}

func truncate(value string, max int) string {
	if len(value) <= max {
		return value
	}
	return value[:max] + "..."
}
