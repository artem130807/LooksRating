import json
from datetime import datetime, timezone
from pathlib import Path

from pyrogram import Client

import config
from services.gift_dispatch import dispatch_ranked_gifts
from services.gift_price_resolver import resolve_gift_ids_by_price
from services.gift_recipients import fetch_vip_top_profiles
from services.vip_top_ranking import PLACE_STAR_PRICES, build_gift_recipients


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _load_last_run() -> datetime | None:
    path: Path = config.VIP_GIFT_STATE_FILE
    if not path.exists():
        return None
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
        raw = payload.get("last_run_at")
        if not raw:
            return None
        return datetime.fromisoformat(raw)
    except (json.JSONDecodeError, ValueError, TypeError):
        return None


def _save_last_run(moment: datetime) -> None:
    path: Path = config.VIP_GIFT_STATE_FILE
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        json.dumps({"last_run_at": moment.astimezone(timezone.utc).isoformat()}, indent=2),
        encoding="utf-8",
    )


def is_job_due() -> bool:
    last_run = _load_last_run()
    if last_run is None:
        return True
    elapsed = _utc_now() - last_run.astimezone(timezone.utc)
    return elapsed.total_seconds() >= config.VIP_GIFT_INTERVAL_DAYS * 86400


async def run_vip_top_gift_job(app: Client) -> None:
    started = _utc_now()
    print(
        f"\033[94m[ VIP JOB ]\033[0m Старт рассылки VIP-топ ({started.astimezone().strftime('%d.%m.%Y %H:%M:%S')})"
    )

    try:
        profiles = fetch_vip_top_profiles()
    except Exception as ex:
        print(f"\033[91m[ VIP JOB ]\033[0m gRPC ошибка: {ex}")
        return

    recipients = build_gift_recipients(profiles)
    print(
        f"\033[94m[ VIP JOB ]\033[0m Профилей из API: {len(profiles)}, "
        f"получателей подарков (топ-5 в категории): {len(recipients)}"
    )

    if not recipients:
        _save_last_run(_utc_now())
        print("\033[93m[ VIP JOB ]\033[0m Нет категорий для рассылки.")
        return

    try:
        gift_ids_by_price = await resolve_gift_ids_by_price(app, set(PLACE_STAR_PRICES))
    except Exception as ex:
        print(f"\033[91m[ VIP JOB ]\033[0m Не удалось получить подарки Telegram: {ex}")
        return

    missing_prices = sorted(set(PLACE_STAR_PRICES) - set(gift_ids_by_price))
    if missing_prices:
        print(f"\033[91m[ VIP JOB ]\033[0m Не найдены подарки для цен: {missing_prices}")
        return

    success, failed = await dispatch_ranked_gifts(
        app,
        recipients,
        gift_ids_by_price,
        send_intro_message=config.VIP_GIFT_SEND_INTRO,
    )

    _save_last_run(_utc_now())
    print(
        f"\033[92m[ VIP JOB ]\033[0m Готово. Успешно: {success}, ошибок: {failed}. "
        f"Следующий запуск через {config.VIP_GIFT_INTERVAL_DAYS} дн."
    )
