# Настройка TGgifts-buyer под LooksRating

## 1. Telegram API (обязательно)

1. Откройте https://my.telegram.org/apps и войдите в **тот аккаунт**, с которого будут уходить подарки.
2. Создайте приложение → скопируйте **api_id** и **api_hash** в `.env`:
   - `API_ID=...`
   - `API_HASH=...`

## 2. Первый вход (сессия)

```bash
cd TGgifts-buyer
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
cp example.env .env
# заполните .env
python main.py
```

При первом запуске Pyrogram запросит **номер телефона** и **код из Telegram**. Сессия сохранится в `data/account.session` — повторный ввод не нужен.

## 3. Звёзды и подарки

- На аккаунте должны быть **Telegram Stars** (оплата подарков).
- В `.env` укажите id подарков:
  - `VIP_GIFT_IDS=123456` — для двухнедельной рассылки VIP-топ
  - или `GIFT_IDS=...` (используется, если `VIP_GIFT_IDS` пустой)

Id подарка можно взять из логов детектора (`APP_MODE=detector`) или каналов вроде @GiftsTracker.

## 4. LooksRating API (gRPC)

Бэкенд должен быть запущен и доступен:

| Окружение | `LOOKSRATING_GRPC_ADDRESS` |
|-----------|----------------------------|
| Docker compose | `api:8080` |
| Локально | `localhost:8080` |

Проверка (из venv):

```bash
python -c "from services.gift_recipients import fetch_vip_top_telegram_ids; print(fetch_vip_top_telegram_ids())"
```

## 5. Режимы `APP_MODE`

| Значение | Поведение |
|----------|-----------|
| `vip_scheduler` | Раз в `VIP_GIFT_INTERVAL_DAYS` (14) — gRPC → id VIP-топ → рассылка подарков |
| `detector` | Только автопокупка новых подарков из каталога |
| `both` | Оба режима параллельно |

Для LooksRating обычно: **`APP_MODE=vip_scheduler`**, **`STARTUP_GIFT_DISPATCH=false`**.

## 6. Канал уведомлений (опционально)

`CHANNEL_ID` — id канала/чата, куда бот шлёт логи (бот должен быть админом или иметь доступ). Можно оставить пустым, если не нужен.

## 7. Docker вместе с LooksRating

В `docker-compose` добавьте сервис buyer с `LOOKSRATING_GRPC_ADDRESS=api:8080` и томом для `data/` (сессия).

## 8. Состояние расписания

Файл `data/json/vip_gift_job_state.json` хранит время последней рассылки. После перезапуска job **не повторится**, пока не пройдёт 14 дней (или удалите файл для принудительного запуска).
