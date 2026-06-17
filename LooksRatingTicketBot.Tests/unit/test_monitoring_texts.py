import importlib.util
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
BOT_ROOT = ROOT / "LooksRatingTicketBot"
sys.path.insert(0, str(BOT_ROOT))


def _load_monitoring_module():
    path = BOT_ROOT / "handlers" / "monitoring.py"
    spec = importlib.util.spec_from_file_location("monitoring_handlers", path)
    module = importlib.util.module_from_spec(spec)
    assert spec.loader is not None
    spec.loader.exec_module(module)
    return module


monitoring = _load_monitoring_module()
_format_alerts = monitoring._format_alerts
_format_status_card = monitoring._format_status_card
_status_icon = monitoring._status_icon


def test_status_icon_mapping():
    assert _status_icon("ok") == "✅"
    assert _status_icon("fail") == "❌"
    assert _status_icon("skip") == "⏭"


def test_format_status_card_empty():
    text = _format_status_card({})
    assert "Проверки ещё не запускались" in text


def test_format_status_card_with_checks():
    text = _format_status_card(
        {
            "lastRun": {
                "overallStatus": "ok",
                "checkedAt": "2026-06-01T12:00:00Z",
                "checks": [{"id": "api_live", "name": "API live", "status": "ok", "message": "live"}],
            }
        }
    )
    assert "API live" in text
    assert "✅" in text


def test_format_status_card_groups_sections():
    text = _format_status_card(
        {
            "lastRun": {
                "overallStatus": "fail",
                "checkedAt": "2026-06-01T12:00:00Z",
                "checks": [
                    {"id": "api_live", "name": "API live", "status": "ok", "message": "live"},
                    {"id": "main_bot", "name": "Main bot", "status": "fail", "message": "down"},
                    {"id": "quartz:error", "name": "Quartz", "status": "fail", "message": "err"},
                ],
            }
        }
    )
    assert "Сервисы" in text
    assert "Telegram" in text
    assert "Планировщик" in text
    assert "Есть проблемы" in text


def test_format_alerts_empty():
    from bot import texts

    assert _format_alerts([]) == texts.MONITORING_NO_ALERTS
