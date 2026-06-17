package monitoring

import (
	"bytes"
	"context"
	"fmt"
	"os/exec"
	"strings"
)

// Имена контейнеров из docker-compose.yml (container_name).
var fixedServiceContainers = map[string]string{
	"bot":          "looksrating-bot",
	"ticket-api":   "looksrating-ticket-api",
	"ticket-bot":   "looksrating-ticket-bot",
	"tgifts-buyer": "looksrating-tgifts-buyer",
}

type DockerRuntime struct {
	composeProject string
}

func NewDockerRuntime(composeProject string) *DockerRuntime {
	return &DockerRuntime{composeProject: strings.TrimSpace(composeProject)}
}

type containerState int

const (
	containerNotFound containerState = iota
	containerStopped
	containerRunning
)

func (d *DockerRuntime) ServiceContainerState(ctx context.Context, service string) (containerState, string, error) {
	names, err := d.resolveContainerNames(ctx, service)
	if err != nil {
		return containerNotFound, "", err
	}
	if len(names) == 0 {
		return containerNotFound, "", nil
	}

	name := names[0]
	running, err := d.isContainerRunning(ctx, name)
	if err != nil {
		return containerNotFound, name, err
	}
	if running {
		return containerRunning, name, nil
	}
	return containerStopped, name, nil
}

func (d *DockerRuntime) resolveContainerNames(ctx context.Context, service string) ([]string, error) {
	if fixed, ok := fixedServiceContainers[service]; ok {
		exists, err := d.containerExists(ctx, fixed)
		if err != nil {
			return nil, err
		}
		if exists {
			return []string{fixed}, nil
		}
	}

	args := []string{
		"ps", "-a",
		"--filter", fmt.Sprintf("label=com.docker.compose.service=%s", service),
		"--format", "{{.Names}}",
	}
	if d.composeProject != "" {
		args = append(args, "--filter", fmt.Sprintf("label=com.docker.compose.project=%s", d.composeProject))
	}

	names, err := d.runLines(ctx, args...)
	if err != nil {
		return nil, err
	}
	if len(names) > 0 {
		return names, nil
	}

	if d.composeProject != "" {
		args = []string{
			"ps", "-a",
			"--filter", fmt.Sprintf("label=com.docker.compose.service=%s", service),
			"--format", "{{.Names}}",
		}
		return d.runLines(ctx, args...)
	}
	return nil, nil
}

func (d *DockerRuntime) containerExists(ctx context.Context, name string) (bool, error) {
	cmd := exec.CommandContext(ctx, "docker", "inspect", "-f", "{{.Id}}", name)
	if err := cmd.Run(); err != nil {
		if strings.Contains(err.Error(), "No such object") ||
			strings.Contains(err.Error(), "not found") {
			return false, nil
		}
		return false, err
	}
	return true, nil
}

func (d *DockerRuntime) isContainerRunning(ctx context.Context, name string) (bool, error) {
	cmd := exec.CommandContext(ctx, "docker", "inspect", "-f", "{{.State.Running}}", name)
	var stdout bytes.Buffer
	cmd.Stdout = &stdout
	if err := cmd.Run(); err != nil {
		return false, err
	}
	return strings.TrimSpace(stdout.String()) == "true", nil
}

func (d *DockerRuntime) TailServiceLogs(ctx context.Context, service string, lines int) (string, error) {
	names, err := d.resolveContainerNames(ctx, service)
	if err != nil {
		return "", err
	}
	if len(names) == 0 {
		return "", fmt.Errorf(
			"контейнер для сервиса %q не найден. Запустите: docker compose up -d %s",
			service, service,
		)
	}

	chunks := make([]string, 0, len(names))
	for _, name := range names {
		chunk, err := d.tailContainerLogs(ctx, name, lines)
		if err != nil {
			return "", err
		}
		if len(names) == 1 {
			return sanitizeLogOutput(chunk), nil
		}
		chunks = append(chunks, fmt.Sprintf("=== %s ===\n%s", name, strings.TrimSpace(chunk)))
	}
	return sanitizeLogOutput(strings.Join(chunks, "\n\n")), nil
}

func (d *DockerRuntime) tailContainerLogs(ctx context.Context, container string, lines int) (string, error) {
	args := []string{"logs", "--tail", fmt.Sprintf("%d", lines), "--timestamps", container}
	cmd := exec.CommandContext(ctx, "docker", args...)
	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		msg := strings.TrimSpace(stderr.String())
		if msg == "" {
			msg = err.Error()
		}
		return "", fmt.Errorf("docker logs %s: %s", container, msg)
	}
	return stdout.String(), nil
}

func (d *DockerRuntime) runLines(ctx context.Context, args ...string) ([]string, error) {
	cmd := exec.CommandContext(ctx, "docker", args...)
	var stdout, stderr bytes.Buffer
	cmd.Stdout = &stdout
	cmd.Stderr = &stderr
	if err := cmd.Run(); err != nil {
		msg := strings.TrimSpace(stderr.String())
		if msg == "" {
			msg = err.Error()
		}
		return nil, fmt.Errorf("docker ps: %s", msg)
	}
	raw := strings.TrimSpace(stdout.String())
	if raw == "" {
		return nil, nil
	}
	return strings.Split(raw, "\n"), nil
}
