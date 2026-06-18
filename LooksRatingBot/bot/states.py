from aiogram.fsm.state import State, StatesGroup


class RegistrationStates(StatesGroup):
    display_choice = State()
    display_name = State()


class FeedSetupStates(StatesGroup):
    city = State()
    age = State()
    gender = State()


class PhotoStates(StatesGroup):
    confirm_create = State()
    custom_city = State()
    custom_age = State()
    custom_gender = State()
    upload = State()


class RecreatePhotoStates(StatesGroup):
    select_target = State()
    custom_city = State()
    custom_age = State()
    custom_gender = State()
    upload = State()
    upload_many = State()


class SettingsStates(StatesGroup):
    confirm_delete = State()
    hide_display_name = State()


class ProfileEditStates(StatesGroup):
    field = State()
    city = State()
    age = State()
    gender = State()


class RatingStates(StatesGroup):
    awaiting_rating = State()


class TicketStates(StatesGroup):
    description = State()
