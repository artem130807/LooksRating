package domain_test

import (
	"testing"

	"looksrating/ticketapi/internal/domain"
)

func TestNewUserSession_Success(t *testing.T) {
	session, err := domain.NewUserSession(123456789)
	if err != nil {
		t.Fatalf("unexpected error: %v", err)
	}
	if session.TelegramID != 123456789 {
		t.Fatalf("telegram id mismatch")
	}
	if session.State != domain.AdminSessionStateStart {
		t.Fatalf("expected start state, got %s", session.State)
	}
}

func TestNewUserSession_InvalidTelegramID(t *testing.T) {
	_, err := domain.NewUserSession(0)
	if err != domain.ErrInvalidTelegramID {
		t.Fatalf("expected ErrInvalidTelegramID, got %v", err)
	}
}

func TestUserSession_SetStateAndPayload(t *testing.T) {
	session, err := domain.NewUserSession(1)
	if err != nil {
		t.Fatal(err)
	}

	if err := session.SetState(domain.AdminSessionStateTicketList); err != nil {
		t.Fatal(err)
	}
	session.SetPayload("ticket_id", "abc-123")
	session.SetPayload("page", "2")

	value, ok := session.GetPayload("ticket_id")
	if !ok || value != "abc-123" {
		t.Fatalf("payload not stored")
	}

	if err := session.LinkAdmin(42); err != nil {
		t.Fatal(err)
	}
	if !session.IsAuthenticated() {
		t.Fatal("expected authenticated session")
	}

	session.ClearAdmin()
	if session.IsAuthenticated() {
		t.Fatal("expected logged out session")
	}
}
