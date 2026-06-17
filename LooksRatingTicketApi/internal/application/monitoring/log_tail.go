package monitoring

import (
	"context"
	"fmt"
	"strings"
)

type LogTailConfig struct {
	Enabled          bool
	ComposeProject   string
	DefaultTailLines int
}

type LogTailService struct {
	cfg    LogTailConfig
	docker *DockerRuntime
}

func NewLogTailService(cfg LogTailConfig, docker *DockerRuntime) *LogTailService {
	if cfg.DefaultTailLines <= 0 {
		cfg.DefaultTailLines = 100
	}
	return &LogTailService{cfg: cfg, docker: docker}
}

func (s *LogTailService) Enabled() bool {
	return s.cfg.Enabled
}

func (s *LogTailService) Tail(ctx context.Context, service string, lines int) (string, error) {
	if !s.cfg.Enabled {
		return "", fmt.Errorf("docker logs недоступны (MONITOR_ENABLE_DOCKER_LOGS=false)")
	}
	if s.docker == nil {
		return "", fmt.Errorf("docker runtime не настроен")
	}
	if lines <= 0 {
		lines = s.cfg.DefaultTailLines
	}
	service = strings.TrimSpace(service)
	if service == "" {
		return "", fmt.Errorf("service обязателен")
	}
	if _, ok := AllowedLogServices[service]; !ok {
		return "", fmt.Errorf("неизвестный сервис: %s", service)
	}
	if lines > MaxLogTailLines {
		lines = MaxLogTailLines
	}

	return s.docker.TailServiceLogs(ctx, service, lines)
}
