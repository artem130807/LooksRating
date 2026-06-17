from aiogram.filters import StateFilter

from bot.states import FeedSetupStates, ProfileEditStates, RatingStates, TicketStates

NOT_DURING_RATING = ~StateFilter(RatingStates.awaiting_rating)
NOT_DURING_RATING_OR_TICKET = ~StateFilter(
    RatingStates.awaiting_rating,
    TicketStates.description,
    FeedSetupStates.city,
    FeedSetupStates.age,
    FeedSetupStates.gender,
)
