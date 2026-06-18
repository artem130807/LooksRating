package monitoring

import (
	"context"
	"log"
	"regexp"
	"strings"
	"time"
)

type QuartzRule struct {
	ID          string
	Title       string
	Fingerprint string
	Severity    string
	Window      func(now time.Time, loc *time.Location) bool
	Pattern     *regexp.Regexp
	Service     string
	Required    bool
}

var (
	quartzErrorPattern   = regexp.MustCompile(`(?i)quartz.*ошибка`)
	seasonFailedPattern  = regexp.MustCompile(`failed=[1-9]`)
	seasonClosePattern   = regexp.MustCompile(`Сезон.*закрыт|Создан сезон`)
	vipRewardsPattern    = regexp.MustCompile(`VIP top biweekly rewards`)
	bestWeekStartPattern = regexp.MustCompile(`Лучшая неделя: старт обновления`)
	vipRewardPeriodDays  = 14
)

func isVipTopRewardDay(t time.Time, loc *time.Location) bool {
	local := t.In(loc)
	epoch := time.Date(2024, 1, 1, 0, 0, 0, 0, loc)
	days := int(local.Truncate(24*time.Hour).Sub(epoch).Hours() / 24)
	if days < 0 {
		return false
	}
	return days%vipRewardPeriodDays == 0
}

func defaultQuartzRules() []QuartzRule {
	return []QuartzRule{
		{
			ID:          "quartz:season:missing",
			Title:       "Сезон не переключился",
			Fingerprint: "quartz:season:missing",
			Severity:    "critical",
			Service:     "api",
			Required:    true,
			Window: func(now time.Time, loc *time.Location) bool {
				t := now.In(loc)
				return t.Day() == 1 && t.Hour() >= 1 && t.Hour() < 3
			},
			Pattern: seasonClosePattern,
		},
		{
			ID:          "quartz:vip_sparks:missing",
			Title:       "VIP sparks rewards не запускались",
			Fingerprint: "quartz:vip_sparks:missing",
			Severity:    "warning",
			Service:     "api",
			Required:    true,
			Window: func(now time.Time, loc *time.Location) bool {
				t := now.In(loc)
				return isVipTopRewardDay(t, loc) && t.Hour() >= 10 && t.Hour() < 11
			},
			Pattern: vipRewardsPattern,
		},
		{
			ID:          "quartz:best_week:missing",
			Title:       "Лучшая неделя не обновлялась",
			Fingerprint: "quartz:best_week:missing",
			Severity:    "warning",
			Service:     "api",
			Required:    true,
			Window: func(now time.Time, loc *time.Location) bool {
				t := now.In(loc)
				return t.Weekday() == time.Monday && t.Hour() >= 0 && t.Hour() < 2
			},
			Pattern: bestWeekStartPattern,
		},
		{
			ID:          "quartz:error",
			Title:       "Ошибка Quartz",
			Fingerprint: "quartz:error",
			Severity:    "critical",
			Service:     "api",
			Required:    false,
			Window:      func(time.Time, *time.Location) bool { return true },
			Pattern:     quartzErrorPattern,
		},
		{
			ID:          "quartz:season:failed",
			Title:       "Сбой season job",
			Fingerprint: "quartz:season:failed",
			Severity:    "critical",
			Service:     "api",
			Required:    false,
			Window:      func(time.Time, *time.Location) bool { return true },
			Pattern:     seasonFailedPattern,
		},
	}
}

func firstMatchingLine(logs string, pattern *regexp.Regexp) string {
	for _, line := range strings.Split(logs, "\n") {
		if pattern.MatchString(line) {
			return line
		}
	}
	return "обнаружено в логах"
}

func (s *Service) evaluateQuartzRules(ctx context.Context, now time.Time) []CheckResult {
	if s.logTail == nil || !s.logTail.Enabled() {
		return nil
	}

	loc := s.cfg.MoscowLocation
	if loc == nil {
		loc = time.UTC
	}

	var apiFetched bool
	var cachedAPI string
	var cachedAPIErr error
	fetchLogs := func(service string) (string, error) {
		if service == "api" {
			if apiFetched {
				return cachedAPI, cachedAPIErr
			}
			apiFetched = true
			logs, err := s.logTail.Tail(ctx, service, s.cfg.LogTailLines)
			cachedAPI = logs
			cachedAPIErr = err
			return logs, err
		}
		return s.logTail.Tail(ctx, service, s.cfg.LogTailLines)
	}

	results := make([]CheckResult, 0, len(s.quartzRules))
	for _, rule := range s.quartzRules {
		inWindow := rule.Window(now, loc)
		alwaysOn := !rule.Required
		if !inWindow && !alwaysOn {
			continue
		}

		started := time.Now()
		logs, err := fetchLogs(rule.Service)
		check := CheckResult{
			ID:       rule.ID,
			Name:     rule.Title,
			Duration: time.Since(started).Round(time.Millisecond).String(),
		}
		if err != nil {
			check.Status = CheckStatusSkip
			check.Message = err.Error()
			results = append(results, check)
			continue
		}

		matched := rule.Pattern.MatchString(logs)
		switch {
		case alwaysOn:
			if matched {
				check.Status = CheckStatusFail
				check.Message = strings.TrimSpace(firstMatchingLine(logs, rule.Pattern))
				s.openQuartzAlert(ctx, rule, check.Message)
			} else {
				check.Status = CheckStatusOK
				check.Message = "ошибок не найдено"
				s.resolveQuartzAlert(ctx, rule)
			}
		case matched:
			check.Status = CheckStatusOK
			check.Message = "паттерн найден в логах"
			s.resolveQuartzAlert(ctx, rule)
		default:
			check.Status = CheckStatusFail
			check.Message = "ожидаемый паттерн не найден в логах"
			s.openQuartzAlert(ctx, rule, check.Message)
		}
		results = append(results, check)
	}

	return results
}

func (s *Service) openQuartzAlert(ctx context.Context, rule QuartzRule, body string) {
	if err := s.alerts.Open(ctx, rule.Fingerprint, rule.Severity, rule.Title, body); err != nil {
		log.Printf("monitor quartz alert open %s: %v", rule.Fingerprint, err)
	}
}

func (s *Service) resolveQuartzAlert(ctx context.Context, rule QuartzRule) {
	if err := s.alerts.Resolve(ctx, rule.Fingerprint, rule.Title); err != nil {
		log.Printf("monitor quartz alert resolve %s: %v", rule.Fingerprint, err)
	}
}
