# LooksRatingBot

Telegram-бот (aiogram 3) для LooksRating API.

## Запуск

1. Запустите API (`LooksRatingApi`, по умолчанию `http://localhost:5149`).
2. Скопируйте `.env.example` в `.env` и укажите `BOT_TOKEN` и при необходимости `API_KEY`.
3. Установите зависимости и запустите бота:

```powershell
cd LooksRatingBot
py -3.13 -m venv venv
.\venv\Scripts\pip install -r requirements.txt
.\venv\Scripts\python.exe main.py
```

## Нет связи с Telegram (`api.telegram.org`)

Ошибка `Cannot connect to host api.telegram.org` — сеть не доходит до серверов Telegram (блокировка, VPN, фаервол).

1. Проверка в PowerShell:
   ```powershell
   Test-NetConnection api.telegram.org -Port 443
   ```
   `TcpTestSucceeded : True` — интернет до Telegram есть.

2. Если `False` — включите VPN или укажите прокси в `.env`:
   ```
   TELEGRAM_PROXY=socks5://127.0.0.1:1080
   ```
   (порт и тип — как в вашем VPN/прокси-клиенте)

3. После смены `.env` перезапустите бота.

## Возможности

- Регистрация по шагам (город → возраст → пол).
- Фото сезона: номинация «как в профиле» или своя.
- Оценка 1–10 (без пропуска), топ-10, жалобы, профиль и «Моя лента».
- Команды: `/start`, `/menu`, `/help`.
