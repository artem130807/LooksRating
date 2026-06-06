# Тест Quartz (ветка TestQuartz)

Отдельный Docker-стек **не трогает** БД ветки Sharaga (`LooksRatingDb` на порту `5432`).

## Быстрый старт

```bash
cd ~/LooksRating
cp .env.example .env   # TELEGRAM_BOT_TOKEN нужен только если поднимаете bot

sudo docker compose up -d --build postgres redis
sudo docker compose up -d --build --scale api=3 api
sudo docker compose logs -f api
```

Kafka и Zookeeper для теста Quartz **не нужны** (VIP / неделя / сезон работают без них).  
В `appsettings.TestQuartz.json` стоит `"Kafka": { "Enabled": false }` — health check и фоновый consumer не стартуют.

Если понадобится Kafka:

```bash
sudo docker compose --profile kafka up -d zookeeper kafka
sudo docker compose up -d --build \
  -e Kafka__Enabled=true \
  -e Kafka__BootstrapServers=kafka:29092 \
  -e KafkaConsumer__PhotoConsume__BootstrapServers=kafka:29092 \
  --scale api=3 api
```

Postgres тестовый: `localhost:5433` / БД `LooksRatingDb_TestQuartz`

API с хоста (опционально, через nginx): http://localhost:8081  
```bash
sudo docker compose --profile gateway up -d api-gateway
```

## Тестовые пользователи

При первом старте API (`ASPNETCORE_ENVIRONMENT=TestQuartz`) автоматически загружаются данные из  
`LooksRatingApi/Infrastructure/TestQuartz/test-quartz-users.json`:

| Группа | Кол-во | Назначение |
|--------|--------|------------|
| Самара | ~20 | VIP-топ, expiry, обычные |
| Москва | ~32 | VIP-топ 12+8, expiry, обычные |
| Санкт-Петербург | ~20 | VIP женщины 12 + мужчины 8 |
| Казань | ~22 | VIP мужчины 12 + обычные |
| Краснодар | ~18 | VIP 10 + обычные |
| Новосибирск | ~18 | VIP 10 + обычные |

**~280 пользователей**, 11 городов. VIP-покупки через бота (`telegram_charge_seed_*`) + `vipPayments` для продления (места 6–10).

Сид **инкрементальный**: новые пользователи + блок `vipPayments` (покупки для уже существующих).

Повторный запуск не дублирует пользователей. Полный сброс — удалить volume `looksrating-testquartz-pgdata` и перезапустить API.

## Расписание (самарское время, UTC+4)

| Job | Время | Cron |
|-----|-------|------|
| VipStatusExpiry | **16:57** | `0 57 16 * * ?` |
| TheBestWeek | **16:58** | `0 58 16 * * ?` |
| NewSeason | **16:59** | `0 59 16 * * ?` |
| NewListSeason | **17:00** | `0 0 17 * * ?` |

Часовой пояс: `Europe/Samara` (+1 ч к Москве). Prod остаётся на `Europe/Moscow`.

При старте API ищите строки `Quartz следующий запуск` — там точное время следующего срабатывания по Самаре.  
После смены cron в `appsettings.TestQuartz.json` триггеры в Postgres пересоздаются автоматически.

`SkipCalendarGuards` — сезонные job можно гонять каждый день (не только 1-е число).

### Prod cron (опционально)

В `appsettings.TestQuartz.json` установите `"MirrorProductionCron": true` — те же cron, что в prod:

| Job | Prod cron | Когда срабатывает |
|-----|-----------|-------------------|
| VipStatusExpiry | каждый час `:00` | каждый час MSK |
| TheBestWeek | `0 0 0 ? * MON` | понедельник 00:00 |
| NewSeason | `0 0 0 1 2-12 ?` | 1-е число фев–дек |
| NewListSeason | `0 0 0 1 1 ?` | 1 января |

С `SkipCalendarGuards: true` сезон/глава всё равно отработают при срабатывании cron.

## Что смотреть в логах API

```bash
sudo docker logs -f looksrating-testquartz-api 2>&1 | grep -iE 'Quartz|VIP expiry|Лучшая неделя|Смена сезона|Создание главы|VIP-топ|TestQuartz сид|gRPC GetTelegramIds'
```

При старте API ищите строки `Quartz планировщик`, `Quartz расписание` и `Quartz следующий запуск`.

```bash
sudo docker compose logs api --tail=300 2>&1 | grep -iE 'QuartzStartup|Quartz \[|TestQuartz сид'
```

Если пусто — смотрите полный хвост (API может ещё сидить данные до старта Quartz):

```bash
sudo docker compose logs api --tail=80
sudo docker compose ps api
```

## Кластер (3 реплики API)

```bash
sudo docker compose up -d --build --scale api=3 api
sudo docker compose ps api   # должно быть 3 контейнера api-1..api-3
```

Если ошибка `port 8081 already allocated` — пересоберите после обновления compose (порт у api убран).

В логах на каждый cron должен быть **один** `Quartz [JobName] старт` (lock + Postgres clustering).

## Тест продления VIP (gRPC)

Пользователи **911006–911012** (Москва, места 6–12): покупка VIP через бота, истекает через ~2 дня.  
Вызов gRPC `GetTelegramIds` должен создать grant-продление для мест 6–10.

```bash
sudo docker compose logs api 2>&1 | grep -iE 'VIP-топ|VIP продлён|gRPC GetTelegramIds'
```

## Пропустили окно 16:57

Отредактируйте `LooksRatingApi/appsettings.TestQuartz.json` — сдвиньте минуты/часы, пересоберите API:

```bash
sudo docker compose up -d --build api
```

Пример на 16:05–16:08: `0 5 16`, `0 6 16`, `0 7 16`, `0 8 16`.

## Остановка и очистка тестовой БД

```bash
sudo docker compose down
sudo docker volume rm looksrating-testquartz-pgdata   # полный сброс
```
