FROM python:3.11-slim

WORKDIR /app

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    && rm -rf /var/lib/apt/lists/*

COPY TGgifts-buyer/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY TGgifts-buyer/adapters/ adapters/
COPY TGgifts-buyer/grpc_gen/ grpc_gen/
COPY TGgifts-buyer/grpc_server/ grpc_server/
COPY TGgifts-buyer/jobs/ jobs/
COPY TGgifts-buyer/locales/ locales/
COPY TGgifts-buyer/proto/ proto/
COPY TGgifts-buyer/scheduler/ scheduler/
COPY TGgifts-buyer/services/ services/
COPY TGgifts-buyer/src/ src/
COPY TGgifts-buyer/utils/ utils/
COPY TGgifts-buyer/config.py TGgifts-buyer/main.py ./

RUN mkdir -p data/json

ENV PYTHONUNBUFFERED=1 \
    APP_MODE=vip_scheduler \
    VIP_GIFT_JOB_ENABLED=true \
    LOOKSRATING_GRPC_ADDRESS=api:8080

CMD ["python", "main.py"]
