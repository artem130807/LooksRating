FROM python:3.11-slim

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY adapters/ adapters/
COPY grpc_gen/ grpc_gen/
COPY jobs/ jobs/
COPY locales/ locales/
COPY proto/ proto/
COPY scheduler/ scheduler/
COPY services/ services/
COPY src/ src/
COPY utils/ utils/
COPY config.py main.py ./

RUN mkdir -p data/json

ENV PYTHONUNBUFFERED=1 \
    APP_MODE=vip_scheduler \
    VIP_GIFT_JOB_ENABLED=true \
    LOOKSRATING_GRPC_ADDRESS=api:8080

CMD ["python", "main.py"]
