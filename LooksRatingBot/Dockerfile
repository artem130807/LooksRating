FROM python:3.11-slim
WORKDIR /app

# Установка системных зависимостей, если необходимы
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Копируем зависимости и устанавливаем их
COPY LooksRatingBot/requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# Копируем исходный код бота
COPY LooksRatingBot/ .

CMD ["python", "main.py"]
