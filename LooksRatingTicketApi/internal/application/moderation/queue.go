package moderation

import (
	"strconv"
	"strings"

	"looksrating/ticketapi/internal/domain"
)

const (
	payloadCity       = "city"
	payloadQueueIndex = "queue_index"
	payloadQueueTotal = "queue_total"
)

type queueSnapshot struct {
	city  string
	index int
	total int
}

func readQueue(payload domain.SessionPayload) queueSnapshot {
	index := 0
	if rawIndex := payload[payloadQueueIndex]; rawIndex != "" {
		if parsed, err := strconv.Atoi(rawIndex); err == nil && parsed >= 0 {
			index = parsed
		}
	}

	total := 0
	if rawTotal := payload[payloadQueueTotal]; rawTotal != "" {
		if parsed, err := strconv.Atoi(rawTotal); err == nil && parsed >= 0 {
			total = parsed
		}
	}

	return queueSnapshot{
		city:  strings.TrimSpace(payload[payloadCity]),
		index: index,
		total: total,
	}
}

func writeQueue(payload domain.SessionPayload, snap queueSnapshot) domain.SessionPayload {
	if payload == nil {
		payload = domain.SessionPayload{}
	}
	payload[payloadCity] = snap.city
	payload[payloadQueueIndex] = strconv.Itoa(snap.index)
	payload[payloadQueueTotal] = strconv.Itoa(snap.total)
	return payload
}

func (snap queueSnapshot) remaining() int {
	if snap.total <= snap.index {
		return 0
	}
	return snap.total - snap.index
}
