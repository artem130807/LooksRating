import asyncio
import logging
from datetime import datetime, timezone
from zoneinfo import ZoneInfo

from aiogram import F, Router
from aiogram.exceptions import TelegramBadRequest
from aiogram.filters import Command, StateFilter
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery, Message

from api.client import ApiError, TicketApiClient
from bot import keyboards, texts
from bot.html_escape import escape_html
from bot.session_sync import is_authenticated
from bot.states import ModerationStates, OpsStates
from handlers.common import load_session

router = Router()
logger = logging.getLogger(__name__)

_LOG_REFRESH_SECONDS = 5
_MSK = ZoneInfo("Europe/Moscow")
_log_refresh_tasks: dict[int, asyncio.Task] = {}

_CHECK_SECTIONS: tuple[tuple[str, tuple[str, ...]], ...] = (
    ("🌐 Сервисы", ("api_live", "api_ready", "api_smoke", "ticket_api", "tgifts_grpc")),
    ("💬 Telegram", ("main_bot",)),
    ("⏱ Планировщик", ()),
)


def _status_icon(status: str) -> str:
    return {
        "ok": "✅",
        "fail": "❌",
        "skip": "⏭",
    }.get(status, "❔")


def _overall_badge(overall: str) -> str:
    if overall == "ok":
        return "✅ <b>Всё в порядке</b>"
    if overall == "fail":
        return "❌ <b>Есть проблемы</b>"
    return f"❔ <b>{escape_html(overall)}</b>"


def _format_checked_at(raw: str) -> str:
    if not raw or raw == "—":
        return "—"
    try:
        parsed = datetime.fromisoformat(str(raw).replace("Z", "+00:00"))
        if parsed.tzinfo is None:
            parsed = parsed.replace(tzinfo=timezone.utc)
        return parsed.astimezone(_MSK).strftime("%d.%m.%Y %H:%M MSK")
    except ValueError:
        return escape_html(str(raw))


def _group_checks(checks: list[dict]) -> list[tuple[str, list[dict]]]:
    by_id = {str(c.get("id") or ""): c for c in checks}
    used: set[str] = set()
    sections: list[tuple[str, list[dict]]] = []

    for title, ids in _CHECK_SECTIONS:
        items = [by_id[i] for i in ids if i in by_id]
        if items:
            sections.append((title, items))
            used.update(ids)

    quartz = [c for c in checks if str(c.get("id") or "").startswith("quartz:")]
    if quartz:
        sections.append(("⏱ Планировщик", quartz))
        used.update(str(c.get("id") or "") for c in quartz)

    other = [c for c in checks if str(c.get("id") or "") not in used]
    if other:
        sections.append(("📋 Прочее", other))
    return sections


def _summarize_checks(checks: list[dict]) -> str:
    ok = fail = skip = 0
    for check in checks:
        status = str(check.get("status") or "")
        if status == "ok":
            ok += 1
        elif status == "fail":
            fail += 1
        elif status == "skip":
            skip += 1
    parts = [f"✅ {ok}"]
    if fail:
        parts.append(f"❌ {fail}")
    if skip:
        parts.append(f"⏭ {skip}")
    return " · ".join(parts)


def _format_check_line(check: dict) -> str:
    icon = _status_icon(str(check.get("status") or ""))
    name = escape_html(str(check.get("name") or check.get("id") or "?"))
    message = str(check.get("message") or "").strip()
    if message:
        return f"{icon} {name} — <i>{escape_html(message)}</i>"
    return f"{icon} {name}"


def _format_status_card(payload: dict) -> str:
    last_run = payload.get("lastRun") or {}
    checks = last_run.get("checks") or []
    checked_at = _format_checked_at(str(last_run.get("checkedAt") or "—"))
    overall = str(last_run.get("overallStatus") or "—")

    lines = [
        "<b>📊 Статус системы</b>",
        "",
        _overall_badge(overall),
        f"🕐 Проверено: <code>{checked_at}</code>",
    ]
    if not checks:
        lines.append("")
        lines.append("Проверки ещё не запускались. Нажмите «🔄 Проверить».")
        return "\n".join(lines)

    lines.append(f"📈 Итого: {_summarize_checks(checks)}")
    for section_title, section_checks in _group_checks(checks):
        lines.append("")
        lines.append(f"<b>{section_title}</b>")
        for check in section_checks:
            lines.append(_format_check_line(check))
    return "\n".join(lines)


def _format_alerts(alerts: list[dict]) -> str:
    if not alerts:
        return texts.MONITORING_NO_ALERTS

    critical = [a for a in alerts if str(a.get("severity") or "").lower() == "critical"]
    warning = [a for a in alerts if str(a.get("severity") or "").lower() != "critical"]

    lines = [f"<b>🔔 Открытые алерты</b> ({len(alerts)})", ""]
    if critical:
        lines.append("<b>🔴 Критичные</b>")
        lines.extend(_format_alert_items(critical))
        lines.append("")
    if warning:
        lines.append("<b>🟡 Предупреждения</b>")
        lines.extend(_format_alert_items(warning))

    if len(alerts) > 20:
        lines.append(f"\n… показаны первые 20 из {len(alerts)}")
    return "\n".join(lines)


def _format_alert_items(alerts: list[dict]) -> list[str]:
    lines: list[str] = []
    for alert in alerts[:20]:
        title = escape_html(str(alert.get("title") or "?"))
        body = escape_html(str(alert.get("body") or ""))
        seen = _format_checked_at(str(alert.get("firstSeenAt") or ""))
        lines.append(f"• <b>{title}</b>")
        if body:
            lines.append(f"  {body}")
        if seen != "—":
            lines.append(f"  <i>{seen}</i>")
    return lines


def _truncate_log(text: str, limit: int = 3600) -> str:
    if len(text) <= limit:
        return text
    return "…\n" + text[-limit:]


def _service_label(service: str) -> str:
    return keyboards.LOG_SERVICES.get(service, service)


async def _stop_log_refresh(chat_id: int) -> None:
    task = _log_refresh_tasks.pop(chat_id, None)
    if task and not task.done():
        task.cancel()
        try:
            await task
        except asyncio.CancelledError:
            pass


async def _count_open_alerts(api: TicketApiClient, telegram_id: int) -> int:
    try:
        alerts = await api.monitoring_alerts(telegram_id)
        return len(alerts)
    except ApiError:
        return 0


async def _require_authenticated_callback(callback: CallbackQuery, api: TicketApiClient) -> bool:
    session = await load_session(api, callback.from_user.id)
    if is_authenticated(session):
        return True
    if callback.message:
        await callback.message.answer(
            "Сначала войдите в аккаунт администратора.",
            reply_markup=keyboards.start_unauthenticated(),
        )
    else:
        await callback.answer("Требуется вход администратора", show_alert=True)
    return False


async def _require_authenticated(message: Message, api: TicketApiClient) -> bool:
    session = await load_session(api, message.from_user.id)
    if is_authenticated(session):
        return True
    await message.answer(
        "Сначала войдите в аккаунт администратора.",
        reply_markup=keyboards.start_unauthenticated(),
    )
    return False


async def show_ops_hub(message: Message, api: TicketApiClient, telegram_id: int) -> None:
    count = await _count_open_alerts(api, telegram_id)
    await message.answer(
        texts.OPS_MENU_INTRO,
        reply_markup=keyboards.ops_hub(count),
    )


async def show_status(
    target: Message,
    api: TicketApiClient,
    telegram_id: int,
    *,
    edit: bool = False,
) -> None:
    try:
        payload = await api.monitoring_status(telegram_id)
    except ApiError as exc:
        text = f"Не удалось загрузить статус: {escape_html(exc.message)}"
        markup = keyboards.ops_hub()
        if edit:
            await target.edit_text(text, reply_markup=markup)
        else:
            await target.answer(text, reply_markup=markup)
        return

    text = _format_status_card(payload)
    markup = keyboards.ops_status_actions()
    if edit:
        try:
            await target.edit_text(text, reply_markup=markup)
        except TelegramBadRequest:
            await target.answer(text, reply_markup=markup)
    else:
        await target.answer(text, reply_markup=markup)


async def run_checks(
    target: Message,
    api: TicketApiClient,
    telegram_id: int,
    *,
    edit: bool = False,
) -> None:
    loading = texts.MONITORING_RUN_STARTED
    if edit:
        try:
            await target.edit_text(loading)
        except TelegramBadRequest:
            pass
    else:
        await target.answer(loading)

    try:
        payload = await api.monitoring_run(telegram_id)
    except ApiError as exc:
        text = f"Проверка не удалась: {escape_html(exc.message)}"
        markup = keyboards.ops_hub()
        if edit:
            await target.edit_text(text, reply_markup=markup)
        else:
            await target.answer(text, reply_markup=markup)
        return

    text = _format_status_card({"lastRun": payload})
    markup = keyboards.ops_status_actions()
    if edit:
        try:
            await target.edit_text(text, reply_markup=markup)
        except TelegramBadRequest:
            await target.answer(text, reply_markup=markup)
    else:
        await target.answer(text, reply_markup=markup)


async def show_alerts(
    target: Message,
    api: TicketApiClient,
    telegram_id: int,
    *,
    edit: bool = False,
) -> None:
    try:
        alerts = await api.monitoring_alerts(telegram_id)
    except ApiError as exc:
        text = f"Не удалось загрузить алерты: {escape_html(exc.message)}"
        markup = keyboards.ops_hub()
        if edit:
            await target.edit_text(text, reply_markup=markup)
        else:
            await target.answer(text, reply_markup=markup)
        return

    text = _format_alerts(alerts)
    markup = keyboards.ops_alerts_actions()
    if edit:
        try:
            await target.edit_text(text, reply_markup=markup)
        except TelegramBadRequest:
            await target.answer(text, reply_markup=markup)
    else:
        await target.answer(text, reply_markup=markup)


async def show_log_service_picker(target: Message, *, edit: bool = False) -> None:
    markup = keyboards.monitoring_log_services()
    if edit:
        await target.edit_text(texts.MONITORING_PICK_SERVICE, reply_markup=markup)
    else:
        await target.answer(texts.MONITORING_PICK_SERVICE, reply_markup=markup)


async def _render_logs_message(api: TicketApiClient, telegram_id: int, service: str) -> str:
    data = await api.monitoring_logs(telegram_id, service=service, tail=80)
    enabled = bool(data.get("enabled"))
    lines = str(data.get("lines") or "")
    label = _service_label(service)
    header = f"<b>📜 {label}</b>\n<i>последние 80 строк · авто 5с</i>\n"
    if not enabled:
        return header + "\n" + escape_html(lines)
    if _looks_like_log_error(lines):
        return header + "\n⚠️ " + escape_html(lines)
    body = escape_html(_truncate_log(lines)) if lines else "(пусто)"
    return header + f"<pre>{body}</pre>"


def _looks_like_log_error(lines: str) -> bool:
    low = lines.lower()
    return any(
        phrase in low
        for phrase in (
            "не найден",
            "docker logs",
            "недоступны",
            "не настроен",
            "no such container",
        )
    )


async def _start_log_view(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    telegram_id: int,
    service: str,
) -> None:
    await _stop_log_refresh(message.chat.id)
    await state.set_state(OpsStates.viewing_logs)
    await state.update_data(log_service=service, log_auto_refresh=True)

    try:
        text = await _render_logs_message(api, telegram_id, service)
    except ApiError as exc:
        await message.answer(
            f"Не удалось загрузить логи: {escape_html(exc.message)}",
            reply_markup=keyboards.monitoring_log_services(),
        )
        return

    sent = await message.answer(text, reply_markup=keyboards.monitoring_log_actions(True))
    await state.update_data(log_message_id=sent.message_id)
    _schedule_log_refresh(message, state, api, telegram_id, service)


def _schedule_log_refresh(
    message: Message,
    state: FSMContext,
    api: TicketApiClient,
    telegram_id: int,
    service: str,
) -> None:
    chat_id = message.chat.id

    async def refresh_loop() -> None:
        while True:
            await asyncio.sleep(_LOG_REFRESH_SECONDS)
            data = await state.get_data()
            if data.get("log_service") != service:
                return
            if not data.get("log_auto_refresh", True):
                continue
            message_id = data.get("log_message_id")
            if not message_id:
                return
            try:
                updated = await _render_logs_message(api, telegram_id, service)
                auto = bool(data.get("log_auto_refresh", True))
                await message.bot.edit_message_text(
                    updated,
                    chat_id=chat_id,
                    message_id=message_id,
                    reply_markup=keyboards.monitoring_log_actions(auto),
                )
            except TelegramBadRequest:
                return
            except ApiError:
                continue
            except asyncio.CancelledError:
                raise
            except Exception:
                logger.exception("Log auto-refresh failed for %s", service)

    _log_refresh_tasks[chat_id] = asyncio.create_task(refresh_loop())


async def handle_ops_entry(message: Message, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated(message, api):
        return
    await _stop_log_refresh(message.chat.id)
    await state.set_state(OpsStates.viewing_hub)
    await show_ops_hub(message, api, message.from_user.id)


# ——— Commands ———

@router.message(Command("ops", "monitoring", "status"))
async def on_ops_command(message: Message, state: FSMContext, api: TicketApiClient) -> None:
    await handle_ops_entry(message, state, api)


# ——— Ops hub callbacks ———

@router.callback_query(F.data == keyboards.CALLBACK_OPS_HOME)
async def on_ops_home(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer()
    if not callback.message:
        return
    await _stop_log_refresh(callback.message.chat.id)
    await state.set_state(OpsStates.viewing_hub)
    count = await _count_open_alerts(api, callback.from_user.id)
    await callback.message.edit_text(texts.OPS_MENU_INTRO, reply_markup=keyboards.ops_hub(count))


@router.callback_query(F.data == keyboards.CALLBACK_OPS_STATUS)
async def on_ops_status(callback: CallbackQuery, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer()
    if callback.message:
        await show_status(callback.message, api, callback.from_user.id, edit=True)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_RUN)
async def on_ops_run(callback: CallbackQuery, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer("Запуск…")
    if callback.message:
        await run_checks(callback.message, api, callback.from_user.id, edit=True)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_ALERTS)
async def on_ops_alerts(callback: CallbackQuery, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer()
    if callback.message:
        await show_alerts(callback.message, api, callback.from_user.id, edit=True)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_LOGS)
async def on_ops_logs_menu(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer()
    if callback.message:
        await _stop_log_refresh(callback.message.chat.id)
        await state.set_state(OpsStates.viewing_hub)
        await show_log_service_picker(callback.message, edit=True)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_HELP)
async def on_ops_help(callback: CallbackQuery, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer()
    if callback.message:
        await callback.message.edit_text(texts.OPS_HELP, reply_markup=keyboards.ops_hub())


# ——— Logs callbacks ———

@router.callback_query(F.data.startswith(keyboards.CALLBACK_OPS_LOG_SERVICE))
async def on_log_service_selected(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    service = (callback.data or "").removeprefix(keyboards.CALLBACK_OPS_LOG_SERVICE)
    if service not in keyboards.LOG_SERVICES:
        await callback.answer("Неизвестный сервис")
        return
    await callback.answer()
    if callback.message:
        await _start_log_view(callback.message, state, api, callback.from_user.id, service)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_LOG_REFRESH)
async def on_log_refresh(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    data = await state.get_data()
    service = data.get("log_service")
    if not service or not callback.message:
        await callback.answer("Нет активного просмотра")
        return
    try:
        text = await _render_logs_message(api, callback.from_user.id, str(service))
        auto = bool(data.get("log_auto_refresh", True))
        await callback.message.edit_text(text, reply_markup=keyboards.monitoring_log_actions(auto))
        await callback.answer("Обновлено")
    except ApiError as exc:
        await callback.answer(exc.message or "Ошибка", show_alert=True)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_LOG_AUTO)
async def on_log_auto_toggle(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    data = await state.get_data()
    auto = not bool(data.get("log_auto_refresh", True))
    await state.update_data(log_auto_refresh=auto)
    if callback.message:
        await callback.message.edit_reply_markup(
            reply_markup=keyboards.monitoring_log_actions(auto),
        )
    await callback.answer("Авто-обновление включено" if auto else "Авто-обновление выключено")


@router.callback_query(F.data == keyboards.CALLBACK_OPS_LOG_BACK)
async def on_log_back(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    if callback.message:
        await _stop_log_refresh(callback.message.chat.id)
    await state.set_state(OpsStates.viewing_hub)
    await state.update_data(log_service=None, log_message_id=None)
    await callback.answer()
    if callback.message:
        await show_log_service_picker(callback.message, edit=True)


@router.callback_query(F.data == keyboards.CALLBACK_OPS_LOG_HOME)
async def on_log_home(callback: CallbackQuery, state: FSMContext, api: TicketApiClient) -> None:
    if not await _require_authenticated_callback(callback, api):
        await callback.answer()
        return
    await callback.answer()
    if not callback.message:
        return
    await _stop_log_refresh(callback.message.chat.id)
    await state.set_state(OpsStates.viewing_hub)
    count = await _count_open_alerts(api, callback.from_user.id)
    await callback.message.edit_text(texts.OPS_MENU_INTRO, reply_markup=keyboards.ops_hub(count))
