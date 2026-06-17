package domain

import "fmt"

// AdminSessionState — FSM админ-бота (жалобы пользователей).
type AdminSessionState string

const (
	AdminSessionStateStart            AdminSessionState = "start"
	AdminSessionStateAwaitingLogin    AdminSessionState = "awaiting_login"
	AdminSessionStateAwaitingPassword AdminSessionState = "awaiting_password"
	AdminSessionStateAuthenticated    AdminSessionState = "authenticated"
	AdminSessionStateAwaitingCity     AdminSessionState = "awaiting_city"
	AdminSessionStateModerating       AdminSessionState = "moderating"
	AdminSessionStateTicketList       AdminSessionState = "ticket_list"
	AdminSessionStateTicketDetail     AdminSessionState = "ticket_detail"
	AdminSessionStateIdle             AdminSessionState = "idle"
)

func (s AdminSessionState) IsValid() bool {
	switch s {
	case AdminSessionStateStart,
		AdminSessionStateAwaitingLogin,
		AdminSessionStateAwaitingPassword,
		AdminSessionStateAuthenticated,
		AdminSessionStateAwaitingCity,
		AdminSessionStateModerating,
		AdminSessionStateTicketList,
		AdminSessionStateTicketDetail,
		AdminSessionStateIdle:
		return true
	default:
		return false
	}
}

func (s AdminSessionState) String() string {
	return string(s)
}

func ParseAdminSessionState(value string) (AdminSessionState, error) {
	state := AdminSessionState(value)
	if !state.IsValid() {
		return "", fmt.Errorf("недопустимое состояние сессии: %q", value)
	}
	return state, nil
}
