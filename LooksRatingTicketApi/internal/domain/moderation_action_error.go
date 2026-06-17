package domain

type ModerationActionError struct {
	Message string
}

func (e *ModerationActionError) Error() string {
	if e != nil && e.Message != "" {
		return e.Message
	}
	return ErrModerationActionFailed.Error()
}
