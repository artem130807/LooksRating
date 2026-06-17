package config

import (
	"fmt"
	"os"
	"strconv"
	"time"
)

type Config struct {
	DatabaseURL         string
	HTTPAddr            string
	LooksRatingHTTPBase string
	LooksRatingGRPCAddr string
	LooksRatingAPIKey   string
	APIKey              string
	HTTPTimeout         time.Duration

	MonitorEnabled              bool
	MonitorInterval             time.Duration
	MonitorAlertCooldown        time.Duration
	MonitorLogTailLines         int
	MonitorDockerComposeProject string
	MonitorEnableDockerLogs     bool
	LooksRatingBotToken         string
	MonitorTgiftsEnabled        bool
	MonitorTgiftsGRPCAddr       string
	MonitorTicketHealthURL      string
}

func Load() (Config, error) {
	cfg := Config{
		DatabaseURL:                 os.Getenv("TICKET_DATABASE_URL"),
		HTTPAddr:                    getenv("TICKET_HTTP_ADDR", ":8090"),
		LooksRatingHTTPBase:         getenv("LOOKSRATING_HTTP_BASE_URL", "http://api:8080"),
		LooksRatingGRPCAddr:         getenv("LOOKSRATING_GRPC_ADDRESS", "api:8080"),
		LooksRatingAPIKey:           os.Getenv("LOOKSRATING_API_KEY"),
		APIKey:                      os.Getenv("TICKET_API_KEY"),
		HTTPTimeout:                 15 * time.Second,
		MonitorEnabled:              getenvBool("MONITOR_ENABLED", true),
		MonitorInterval:             time.Duration(getenvInt("MONITOR_INTERVAL_SECONDS", 60)) * time.Second,
		MonitorAlertCooldown:        time.Duration(getenvInt("MONITOR_ALERT_COOLDOWN_MINUTES", 30)) * time.Minute,
		MonitorLogTailLines:         getenvInt("MONITOR_LOG_TAIL_LINES", 100),
		MonitorDockerComposeProject: getenv("MONITOR_DOCKER_COMPOSE_PROJECT", "looksrating"),
		MonitorEnableDockerLogs:     getenvBool("MONITOR_ENABLE_DOCKER_LOGS", false),
		LooksRatingBotToken:         os.Getenv("LOOKS_RATING_BOT_TOKEN"),
		MonitorTgiftsEnabled:        getenvBool("MONITOR_TGIFTS_ENABLED", true),
		MonitorTgiftsGRPCAddr:       getenv("MONITOR_TGIFTS_GRPC_ADDR", "tgifts-buyer:50051"),
		MonitorTicketHealthURL:      getenv("MONITOR_TICKET_HEALTH_URL", "http://127.0.0.1:8090/health"),
	}

	if cfg.DatabaseURL == "" {
		return Config{}, fmt.Errorf("TICKET_DATABASE_URL обязателен")
	}
	if cfg.LooksRatingAPIKey == "" {
		return Config{}, fmt.Errorf("LOOKSRATING_API_KEY обязателен")
	}
	if cfg.APIKey == "" {
		return Config{}, fmt.Errorf("TICKET_API_KEY обязателен")
	}

	return cfg, nil
}

func getenv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

func getenvInt(key string, fallback int) int {
	raw := os.Getenv(key)
	if raw == "" {
		return fallback
	}
	value, err := strconv.Atoi(raw)
	if err != nil {
		return fallback
	}
	return value
}

func getenvBool(key string, fallback bool) bool {
	raw := os.Getenv(key)
	if raw == "" {
		return fallback
	}
	switch raw {
	case "1", "true", "TRUE", "yes", "YES", "on", "ON":
		return true
	case "0", "false", "FALSE", "no", "NO", "off", "OFF":
		return false
	default:
		return fallback
	}
}
