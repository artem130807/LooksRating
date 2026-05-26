GENDER_API_NAMES = {
    1: "Male",
    2: "Female",
    3: "MaleFamale",
}


def gender_to_api(value: int) -> str:
    return GENDER_API_NAMES[value]
