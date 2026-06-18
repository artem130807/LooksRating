from __future__ import annotations

from aiogram import F, Router
from aiogram.types import CallbackQuery, InlineKeyboardButton, InlineKeyboardMarkup

from api.client import ApiError, LooksRatingApiClient
from bot import texts
from bot.services import format_api_error, format_city_display, format_rating_display

router = Router()


def _reviewer_field(reviewer: dict, *keys: str):
    for key in keys:
        value = reviewer.get(key)
        if value is not None:
            return value
    return None


def _reviewers_keyboard(notification_id: str, reviewers: list[dict]) -> InlineKeyboardMarkup:
    rows: list[list[InlineKeyboardButton]] = []
    for index, reviewer in enumerate(reviewers[:10], start=1):
        profile_id = _reviewer_field(reviewer, "reviewerPhotoProfileId", "ReviewerPhotoProfileId")
        name = str(_reviewer_field(reviewer, "displayName", "DisplayName") or "Участник")
        rating = _reviewer_field(reviewer, "rating", "Rating") or "—"
        if profile_id:
            rows.append(
                [
                    InlineKeyboardButton(
                        text=f"{index}. {name} · ⭐ {rating}/10",
                        callback_data=f"review_milestone:profile:{profile_id}",
                    )
                ]
            )
        else:
            rows.append(
                [
                    InlineKeyboardButton(
                        text=f"{index}. {name} · ⭐ {rating}/10",
                        callback_data="review_milestone:noop",
                    )
                ]
            )

    rows.append([InlineKeyboardButton(text="📱 В меню", callback_data="review_milestone:menu")])
    return InlineKeyboardMarkup(inline_keyboard=rows)


@router.callback_query(F.data.startswith("review_milestone:view:"))
async def review_milestone_view(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    notification_id = callback.data.removeprefix("review_milestone:view:")
    try:
        payload = await api.get_review_milestone_reviewers(notification_id)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return

    reviewers = payload.get("reviewers") if isinstance(payload.get("reviewers"), list) else []
    if not reviewers and isinstance(payload.get("Reviewers"), list):
        reviewers = payload.get("Reviewers")
    if not reviewers:
        await callback.answer("Список оценивших пока пуст.", show_alert=True)
        return

    if callback.message:
        await callback.message.answer(
            texts.REVIEW_MILESTONE_REVIEWERS_HEADER,
            reply_markup=_reviewers_keyboard(notification_id, reviewers),
        )
    await callback.answer()


@router.callback_query(F.data.startswith("review_milestone:profile:"))
async def review_milestone_profile(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    profile_id = callback.data.removeprefix("review_milestone:profile:")
    try:
        profile = await api.get_photo_user_by_id(profile_id)
    except ApiError as exc:
        await callback.answer(format_api_error(exc), show_alert=True)
        return

    if not profile:
        await callback.answer("Профиль участника не найден.", show_alert=True)
        return

    caption = texts.TOP_USER_PROFILE.format(
        name=profile.get("userName") or profile.get("UserName") or "Участник",
        gender=profile.get("gender", "—"),
        age=profile.get("age", "—"),
        city=format_city_display(profile.get("city")),
        rank=profile.get("rank", "—"),
        rating_line=format_rating_display(
            float(profile.get("rating", 0)),
            int(profile.get("ratingCount", 0)),
        ),
    )
    image = profile.get("image")
    if image and callback.message:
        await callback.message.answer_photo(image, caption=caption)
    elif callback.message:
        await callback.message.answer(caption)
    await callback.answer()


@router.callback_query(F.data == "review_milestone:noop")
async def review_milestone_noop(callback: CallbackQuery) -> None:
    await callback.answer("У участника нет фото в этом сезоне.", show_alert=True)


@router.callback_query(F.data == "review_milestone:menu")
async def review_milestone_menu(callback: CallbackQuery) -> None:
    await callback.answer()
    if callback.message:
        await callback.message.answer("Откройте /menu для главного меню.")
