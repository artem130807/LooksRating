FROM golang:1.25-alpine AS build
WORKDIR /src
RUN apk add --no-cache git ca-certificates
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o /ticket-api ./cmd/api && \
    CGO_ENABLED=0 GOOS=linux go build -o /ticket-seed-admins ./cmd/seed-admins

FROM alpine:3.20
RUN apk add --no-cache ca-certificates tzdata curl docker-cli docker-cli-compose
WORKDIR /app
COPY --from=build /ticket-api .
COPY --from=build /ticket-seed-admins .
COPY data/admins.seed.json ./data/admins.seed.json
EXPOSE 8090
HEALTHCHECK --interval=15s --timeout=5s --retries=5 CMD curl -f http://127.0.0.1:8090/health || exit 1
CMD ["./ticket-api"]
