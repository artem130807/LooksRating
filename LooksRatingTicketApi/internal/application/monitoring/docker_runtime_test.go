package monitoring

import "testing"

func TestFixedServiceContainers(t *testing.T) {
	if fixedServiceContainers["bot"] != "looksrating-bot" {
		t.Fatalf("unexpected bot container: %s", fixedServiceContainers["bot"])
	}
	if fixedServiceContainers["tgifts-buyer"] != "looksrating-tgifts-buyer" {
		t.Fatalf("unexpected tgifts container: %s", fixedServiceContainers["tgifts-buyer"])
	}
}

func TestSanitizeLogOutputRedactsBearer(t *testing.T) {
	raw := "auth header Bearer secret-token-123\n"
	out := sanitizeLogOutput(raw)
	if out == raw {
		t.Fatalf("expected bearer redaction, got %q", out)
	}
}
