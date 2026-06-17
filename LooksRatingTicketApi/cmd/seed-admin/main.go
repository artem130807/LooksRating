package main

import (
	"context"
	"flag"
	"log"
	"os"

	"looksrating/ticketapi/internal/application/session"
	"looksrating/ticketapi/internal/infrastructure/hasher"
	"looksrating/ticketapi/internal/infrastructure/persistence"
)

func main() {
	login := flag.String("login", "", "логин администратора")
	password := flag.String("password", "", "пароль администратора")
	telegramID := flag.Int64("telegram-id", 0, "опциональный telegram id администратора")
	flag.Parse()

	if *login == "" || *password == "" {
		log.Fatal("укажите --login и --password")
	}

	databaseURL := os.Getenv("TICKET_DATABASE_URL")
	if databaseURL == "" {
		log.Fatal("TICKET_DATABASE_URL обязателен")
	}

	db, err := persistence.OpenPostgres(databaseURL)
	if err != nil {
		log.Fatalf("database: %v", err)
	}

	sessionRepo := persistence.NewUserSessionRepository(db)
	adminRepo := persistence.NewAdminRepository(db)
	passwordHasher := hasher.NewBcryptHasher(0)
	sessionService := session.NewService(sessionRepo, adminRepo, passwordHasher)

	var tgPtr *int64
	if *telegramID > 0 {
		tgPtr = telegramID
	}

	admin, err := sessionService.RegisterAdmin(context.Background(), *login, *password, tgPtr)
	if err != nil {
		log.Fatalf("register admin: %v", err)
	}

	log.Printf("admin created: id=%d login=%s", admin.ID, admin.FirstName)
}
