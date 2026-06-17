from aiogram.fsm.state import State, StatesGroup


class AuthStates(StatesGroup):
    awaiting_login = State()
    awaiting_password = State()


class ModerationStates(StatesGroup):
    selecting_city = State()
    moderating = State()
    confirming_delete_profile = State()
    confirming_delete_account = State()


class OpsStates(StatesGroup):
    viewing_hub = State()
    viewing_logs = State()
