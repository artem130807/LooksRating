from __future__ import annotations

from aiogram import F, Router
from aiogram.fsm.context import FSMContext
from aiogram.types import CallbackQuery

from api.client import LooksRatingApiClient
from bot import texts
from bot.services import AGE_ALL
from handlers.photo import start_nomination_flow
from handlers.top_browse import _restore_tops_menu, _send_gender_pick

router = Router()


@router.callback_query(F.data.startswith("season_rollover:results:"))
async def season_rollover_results(callback: CallbackQuery, api: LooksRatingApiClient) -> None:
    season_id = callback.data.removeprefix("season_rollover:results:")
    if not season_id:
        await _restore_tops_menu(callback.message)
        await callback.answer(texts.TOP_LIST_REFRESH_REQUIRED, show_alert=True)
        return

    user = await api.get_user(callback.from_user.id)
    if not user or not user.get("hasRecommendationSettings"):
        await callback.answer(texts.TOP_SETUP_REQUIRED, show_alert=True)
        return

    age = user.get("age")
    if not isinstance(age, int) or age < AGE_ALL or age > 100:
        await callback.answer(texts.TOP_AGE_REQUIRED, show_alert=True)
        return

    if callback.message:
        await _send_gender_pick(
            callback.message,
            age=age,
            scope="season",
            season_id=season_id,
            edit=True,
        )
    await callback.answer()


@router.callback_query(F.data == "season_rollover:add_photo")
async def season_rollover_add_photo(
    callback: CallbackQuery,
    state: FSMContext,
    api: LooksRatingApiClient,
) -> None:
    if not callback.message:
        await callback.answer()
        return

    await start_nomination_flow(
        callback.message,
        state,
        api,
        recreate=False,
        from_settings=False,
    )
    await callback.answer()
