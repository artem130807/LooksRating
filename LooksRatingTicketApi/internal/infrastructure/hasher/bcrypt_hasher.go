package hasher

import (
	"fmt"

	"golang.org/x/crypto/bcrypt"

	"looksrating/ticketapi/internal/domain/contracts"
)

type BcryptHasher struct {
	cost int
}

func NewBcryptHasher(cost int) contracts.PasswordHasher {
	if cost < bcrypt.MinCost {
		cost = bcrypt.DefaultCost
	}
	return &BcryptHasher{cost: cost}
}

func (h *BcryptHasher) Hash(password string) (string, error) {
	if password == "" {
		return "", fmt.Errorf("пароль не может быть пустым")
	}
	hash, err := bcrypt.GenerateFromPassword([]byte(password), h.cost)
	if err != nil {
		return "", err
	}
	return string(hash), nil
}

func (h *BcryptHasher) Verify(hashedPassword, plainPassword string) error {
	return bcrypt.CompareHashAndPassword([]byte(hashedPassword), []byte(plainPassword))
}
