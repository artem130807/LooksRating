package contracts

type PasswordHasher interface {
	Hash(password string) (string, error)
	Verify(hashedPassword, plainPassword string) error
}
