package main

import (
	"context"
	"encoding/json"
	"flag"
	"log"
	"os"

	"looksrating/ticketapi/internal/application/session"
	"looksrating/ticketapi/internal/infrastructure/hasher"
	"looksrating/ticketapi/internal/infrastructure/persistence"
)

type adminSeedRecord struct {
	Login    string `json:"login"`
	Password string `json:"password"`
}

func main() {
	filePath := flag.String("file", "data/admins.seed.json", "путь к JSON со списком администраторов")
	flag.Parse()

	databaseURL := os.Getenv("TICKET_DATABASE_URL")
	if databaseURL == "" {
		log.Fatal("TICKET_DATABASE_URL обязателен")
	}

	raw, err := os.ReadFile(*filePath)
	if err != nil {
		log.Fatalf("read seed file: %v", err)
	}

	var records []adminSeedRecord
	if err := json.Unmarshal(raw, &records); err != nil {
		log.Fatalf("parse seed file: %v", err)
	}
	if len(records) == 0 {
		log.Fatal("seed file is empty")
	}

	db, err := persistence.OpenPostgres(databaseURL)
	if err != nil {
		log.Fatalf("database: %v", err)
	}

	sessionRepo := persistence.NewUserSessionRepository(db)
	adminRepo := persistence.NewAdminRepository(db)
	passwordHasher := hasher.NewBcryptHasher(0)
	sessionService := session.NewService(sessionRepo, adminRepo, passwordHasher)

	ctx := context.Background()
	created := 0
	skipped := 0

	for _, record := range records {
		login := record.Login
		password := record.Password
		if login == "" || password == "" {
			log.Fatalf("login and password are required in seed file")
		}

		existing, err := adminRepo.GetByFirstName(ctx, login)
		if err != nil {
			log.Fatalf("lookup admin %s: %v", login, err)
		}
		if existing != nil {
			log.Printf("skip existing admin: %s", login)
			skipped++
			continue
		}

		admin, err := sessionService.RegisterAdmin(ctx, login, password, nil)
		if err != nil {
			log.Fatalf("create admin %s: %v", login, err)
		}

		log.Printf("created admin: id=%d login=%s", admin.ID, admin.FirstName)
		created++
	}

	log.Printf("seed complete: created=%d skipped=%d total=%d", created, skipped, len(records))
}
