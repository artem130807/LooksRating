package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	"looksrating/ticketapi/internal/application/moderation"
	"looksrating/ticketapi/internal/application/monitoring"
	"looksrating/ticketapi/internal/application/session"
	"looksrating/ticketapi/internal/config"
	"looksrating/ticketapi/internal/infrastructure/hasher"
	"looksrating/ticketapi/internal/infrastructure/looksratinggrpc"
	"looksrating/ticketapi/internal/infrastructure/persistence"
	"looksrating/ticketapi/internal/infrastructure/tickethttp"
	"looksrating/ticketapi/internal/transport/httpserver"
)

func main() {
	cfg, err := config.Load()
	if err != nil {
		log.Fatalf("config: %v", err)
	}

	db, err := persistence.OpenPostgres(cfg.DatabaseURL)
	if err != nil {
		log.Fatalf("database: %v", err)
	}

	ticketClient := tickethttp.NewClient(cfg.LooksRatingHTTPBase, cfg.LooksRatingAPIKey, cfg.HTTPTimeout)
	moderationActions, err := looksratinggrpc.NewModerationActionsClient(
		cfg.LooksRatingGRPCAddr,
		cfg.LooksRatingAPIKey,
		cfg.HTTPTimeout,
	)
	if err != nil {
		log.Fatalf("grpc client: %v", err)
	}
	defer moderationActions.Close()

	pingCtx, pingCancel := context.WithTimeout(context.Background(), cfg.HTTPTimeout)
	defer pingCancel()
	if err := ticketClient.Ping(pingCtx); err != nil {
		log.Printf("WARNING: LooksRating HTTP API unreachable at %s: %v", cfg.LooksRatingHTTPBase, err)
	} else {
		log.Printf("LooksRating HTTP API reachable at %s", cfg.LooksRatingHTTPBase)
	}
	log.Printf("LooksRating gRPC moderation client configured for %s", cfg.LooksRatingGRPCAddr)

	sessionRepo := persistence.NewUserSessionRepository(db)
	adminRepo := persistence.NewAdminRepository(db)
	monitorRepo := persistence.NewMonitorRepository(db)
	passwordHasher := hasher.NewBcryptHasher(0)
	sessionService := session.NewService(sessionRepo, adminRepo, passwordHasher)
	moderationService := moderation.NewService(ticketClient, moderationActions, sessionRepo)

	moscow, err := time.LoadLocation("Europe/Moscow")
	if err != nil {
		moscow = time.FixedZone("MSK", 3*60*60)
	}

	dockerRuntime := monitoring.NewDockerRuntime(cfg.MonitorDockerComposeProject)
	logTail := monitoring.NewLogTailService(monitoring.LogTailConfig{
		Enabled:          cfg.MonitorEnableDockerLogs,
		ComposeProject:   cfg.MonitorDockerComposeProject,
		DefaultTailLines: cfg.MonitorLogTailLines,
	}, dockerRuntime)
	alertService := monitoring.NewAlertService(monitorRepo, cfg.MonitorAlertCooldown)
	monitorService := monitoring.NewService(
		monitoring.Config{
			Enabled:             cfg.MonitorEnabled,
			Interval:            cfg.MonitorInterval,
			AlertCooldown:       cfg.MonitorAlertCooldown,
			LogTailLines:        cfg.MonitorLogTailLines,
			LooksRatingHTTPBase: cfg.LooksRatingHTTPBase,
			LooksRatingAPIKey:   cfg.LooksRatingAPIKey,
			LooksRatingBotToken: cfg.LooksRatingBotToken,
			TgiftsEnabled:       cfg.MonitorTgiftsEnabled,
			TgiftsGRPCAddr:      cfg.MonitorTgiftsGRPCAddr,
			TicketAPIHealthURL:  cfg.MonitorTicketHealthURL,
			MoscowLocation:      moscow,
		},
		monitorRepo,
		adminRepo,
		alertService,
		logTail,
		dockerRuntime,
		cfg.HTTPTimeout,
	)
	scheduler := monitoring.NewScheduler(monitorService, cfg.MonitorInterval)
	scheduler.Start()

	server := &http.Server{
		Addr:              cfg.HTTPAddr,
		Handler:           httpserver.NewServer(cfg.APIKey, httpserver.NewHandlers(sessionService, moderationService, monitorService)),
		ReadHeaderTimeout: 10 * time.Second,
		ReadTimeout:       30 * time.Second,
		WriteTimeout:      30 * time.Second,
		IdleTimeout:       60 * time.Second,
	}

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	go func() {
		log.Printf(
			"LooksRatingTicketApi listening on %s (http=%s grpc=%s monitor=%v)",
			cfg.HTTPAddr,
			cfg.LooksRatingHTTPBase,
			cfg.LooksRatingGRPCAddr,
			cfg.MonitorEnabled,
		)
		if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
			log.Fatalf("http server: %v", err)
		}
	}()

	<-ctx.Done()
	shutdownCtx, cancel := context.WithTimeout(context.Background(), 10*time.Second)
	defer cancel()
	scheduler.Stop(shutdownCtx)
	if err := server.Shutdown(shutdownCtx); err != nil {
		log.Printf("shutdown: %v", err)
	}
	log.Println("stopped")
}
