import json
import os
from importlib import import_module

import pyfiglet


def info(file_path="data/json/info.json"):
   with open(file_path, "r", encoding="utf-8") as file:
        return json.load(file)


def app_banner(app_name: str) -> str:
    return pyfiglet.figlet_format(app_name, font="slant")


def title(app_info: dict, language: str):
    banner = app_banner(app_info["title"])
    separator = "-" * 80
    description = (
        f"Language: \033[1m{language}\033[0m | "
        f"Build: \033[92mv{app_info['version']}\033[0m | "
        f"TG: @{app_info['publisher']['contact']['channel'][13:]}"
    )

    centered_banner = "\n".join([line.center(80) for line in banner.splitlines()])

    print(separator)
    print(centered_banner)
    print(separator)
    print(f"{description}".center(95))
    print(separator)


def cmd(app_info: dict):
    title_text = f"{app_info['title']} by @{app_info['publisher']['contact']['telegram'][13:]}"
    if os.name == 'nt':
        os.system(f"title {title_text}")


def get_locale(lang: str):
    if not lang:
        lang = 'EN'

    try:
        locale_module = import_module(f'locales.{lang.lower()}')
        return locale_module.LANG[3:], locale_module.CODE
    except ModuleNotFoundError:
        locale_module = import_module('locales.en')
        return locale_module.LANG[3:], locale_module.CODE
