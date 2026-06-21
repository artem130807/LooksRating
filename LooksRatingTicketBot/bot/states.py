from aiogram.fsm.state import State, StatesGroup


class AuthStates(StatesGroup):
    awaiting_login = State()
    awaiting_password = State()


class ModerationStates(StatesGroup):
    selecting_moderation_type = State()
    selecting_city = State()
    selecting_withdrawal_city = State()
    viewing_withdrawal_list = State()
    viewing_withdrawal_detail = State()
    moderating = State()
    confirming_delete_profile = State()
    confirming_delete_account = State()


class OpsStates(StatesGroup):
    viewing_hub = State()
    viewing_logs = State()
