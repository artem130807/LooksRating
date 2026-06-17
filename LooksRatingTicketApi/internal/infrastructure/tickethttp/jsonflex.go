package tickethttp

import (
	"encoding/json"
	"strconv"

	"looksrating/ticketapi/internal/domain/contracts"
)

func decodeString(data map[string]json.RawMessage, keys ...string) string {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var value string
		if err := json.Unmarshal(raw, &value); err == nil && value != "" {
			return value
		}
	}
	return ""
}

func decodeInt(data map[string]json.RawMessage, keys ...string) int {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var value int
		if err := json.Unmarshal(raw, &value); err == nil {
			return value
		}
		var asFloat float64
		if err := json.Unmarshal(raw, &asFloat); err == nil {
			return int(asFloat)
		}
	}
	return 0
}

func decodeInt64(data map[string]json.RawMessage, keys ...string) int64 {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var value int64
		if err := json.Unmarshal(raw, &value); err == nil {
			return value
		}
		var asFloat float64
		if err := json.Unmarshal(raw, &asFloat); err == nil {
			return int64(asFloat)
		}
	}
	return 0
}

func decodeFloat(data map[string]json.RawMessage, keys ...string) float64 {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var value float64
		if err := json.Unmarshal(raw, &value); err == nil {
			return value
		}
	}
	return 0
}

func decodeStringSlice(data map[string]json.RawMessage, keys ...string) []string {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var values []string
		if err := json.Unmarshal(raw, &values); err == nil {
			return values
		}
	}
	return nil
}

func decodeTicketIDs(data map[string]json.RawMessage, keys ...string) []string {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var items []map[string]json.RawMessage
		if err := json.Unmarshal(raw, &items); err != nil {
			continue
		}
		ids := make([]string, 0, len(items))
		for _, item := range items {
			id := decodeString(item, "ticketId", "TicketId", "id", "Id")
			if id != "" {
				ids = append(ids, id)
			}
		}
		return ids
	}
	return nil
}

func decodeTicketPhotos(data map[string]json.RawMessage, keys ...string) []contracts.TicketPhoto {
	for _, key := range keys {
		raw, ok := data[key]
		if !ok || len(raw) == 0 {
			continue
		}
		var items []map[string]json.RawMessage
		if err := json.Unmarshal(raw, &items); err != nil {
			continue
		}
		photos := make([]contracts.TicketPhoto, 0, len(items))
		for _, item := range items {
			photoID := decodeString(item, "photoId", "PhotoId", "id", "Id")
			fileID := decodeString(item, "telegramFileId", "TelegramFileId")
			sortOrder := int32(decodeInt(item, "sortOrder", "SortOrder"))
			if fileID == "" && photoID == "" {
				continue
			}
			photos = append(photos, contracts.TicketPhoto{
				ID:             photoID,
				TelegramFileID: fileID,
				SortOrder:      sortOrder,
			})
		}
		return photos
	}
	return nil
}

func decodeStringFromAny(raw json.RawMessage) string {
	if len(raw) == 0 {
		return ""
	}
	var value string
	if err := json.Unmarshal(raw, &value); err == nil {
		return value
	}
	if unquoted, err := strconv.Unquote(string(raw)); err == nil {
		return unquoted
	}
	return ""
}

func decodeTicketDetail(data map[string]json.RawMessage) *contracts.TicketDetail {
	if len(data) == 0 {
		return nil
	}

	return &contracts.TicketDetail{
		ID:                  decodeString(data, "ticketId", "TicketId", "id", "Id"),
		Description:         decodeString(data, "description", "Description"),
		ReporterTelegramID:  decodeInt64(data, "reporterTelegramId", "ReporterTelegramId"),
		ReporterDisplayName: decodeString(data, "reporterDisplayName", "ReporterDisplayName"),
		ReporterCity:        decodeString(data, "reporterCity", "ReporterCity"),
		PhotoProfileID:      decodeString(data, "photoProfileId", "PhotoProfileId"),
		ProfileTelegramID:   decodeInt64(data, "profileTelegramId", "ProfileTelegramId"),
		ProfileDisplayName:  decodeString(data, "profileDisplayName", "ProfileDisplayName"),
		ProfileCity:         decodeString(data, "profileCity", "ProfileCity"),
		ProfileAge:          int32(decodeInt(data, "profileAge", "ProfileAge")),
		ProfileGender:       decodeString(data, "profileGender", "ProfileGender"),
		ProfileRating:       decodeFloat(data, "profileRating", "ProfileRating"),
		ProfileRatingCount:  int32(decodeInt(data, "profileRatingCount", "ProfileRatingCount")),
		ProfileRank:         decodeString(data, "profileRank", "ProfileRank"),
		Photos:              decodeTicketPhotos(data, "photos", "Photos"),
	}
}
