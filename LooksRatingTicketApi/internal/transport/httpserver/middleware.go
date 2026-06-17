package httpserver

import (
	"log"
	"net/http"
	"strings"
)

func apiKeyMiddleware(expectedKey string, next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path == "/health" {
			next.ServeHTTP(w, r)
			return
		}

		provided := strings.TrimSpace(r.Header.Get("X-Api-Key"))
		if provided == "" || provided != expectedKey {
			writeJSON(w, http.StatusUnauthorized, errorResponse{Error: "неверный API key"})
			return
		}

		next.ServeHTTP(w, r)
	})
}

func recoveryMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		defer func() {
			if recovered := recover(); recovered != nil {
				log.Printf("panic on %s %s: %v", r.Method, r.URL.Path, recovered)
				writeJSON(w, http.StatusInternalServerError, errorResponse{Error: "внутренняя ошибка сервера"})
			}
		}()
		next.ServeHTTP(w, r)
	})
}
