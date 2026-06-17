package monitoring

import "strings"

func sanitizeLogOutput(raw string) string {
	lines := strings.Split(raw, "\n")
	out := make([]string, 0, len(lines))
	for _, line := range lines {
		out = append(out, redactSecrets(line))
	}
	return strings.Join(out, "\n")
}

func redactSecrets(line string) string {
	if strings.Contains(line, "X-Api-Key") {
		parts := strings.SplitN(line, "X-Api-Key", 2)
		return parts[0] + "X-Api-Key: ***"
	}
	lower := strings.ToLower(line)
	if strings.Contains(lower, "bearer ") {
		return "Bearer ***"
	}
	if idx := strings.Index(lower, "token"); idx >= 0 && strings.Contains(line, "=") {
		return line[:idx] + "token=***"
	}
	return line
}
