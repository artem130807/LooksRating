package monitoring

import (
	"context"
	"encoding/json"
	"fmt"
	"io"
	"net"
	"net/http"
	"strings"
	"time"
)

type HTTPProber struct {
	client *http.Client
}

func NewHTTPProber(timeout time.Duration) *HTTPProber {
	return &HTTPProber{
		client: &http.Client{Timeout: timeout},
	}
}

func (p *HTTPProber) Get(ctx context.Context, url string, apiKey string) (int, string, error) {
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return 0, "", err
	}
	if apiKey != "" {
		req.Header.Set("X-Api-Key", apiKey)
	}

	resp, err := p.client.Do(req)
	if err != nil {
		return 0, "", err
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(io.LimitReader(resp.Body, 512))
	return resp.StatusCode, strings.TrimSpace(string(body)), nil
}

func (p *HTTPProber) TelegramGetMe(ctx context.Context, token string) error {
	if token == "" {
		return fmt.Errorf("токен main bot не задан")
	}
	url := fmt.Sprintf("https://api.telegram.org/bot%s/getMe", token)
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, url, nil)
	if err != nil {
		return err
	}
	resp, err := p.client.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	body, _ := io.ReadAll(io.LimitReader(resp.Body, 1024))
	if resp.StatusCode != http.StatusOK {
		return fmt.Errorf("HTTP %d: %s", resp.StatusCode, body)
	}

	var payload struct {
		OK bool `json:"ok"`
	}
	if err := json.Unmarshal(body, &payload); err != nil {
		return err
	}
	if !payload.OK {
		return fmt.Errorf("telegram getMe: ok=false")
	}
	return nil
}

func TCPDial(ctx context.Context, address string, timeout time.Duration) error {
	dialer := net.Dialer{Timeout: timeout}
	conn, err := dialer.DialContext(ctx, "tcp", address)
	if err != nil {
		return err
	}
	return conn.Close()
}
