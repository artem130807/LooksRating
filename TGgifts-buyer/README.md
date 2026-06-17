<h2 align="center">
  Telegram Gifts Auto-Buyer<br/>
</h2>

<div align="center">
  <img src="https://github.com/user-attachments/assets/2c4540b7-4e39-4306-945f-389271123ecc" alt="Preview" width="600px">
</div>

<p align="center">
  <img src="https://img.shields.io/badge/python-3.10+-blue.svg" alt="Python Version">
  <img src="https://img.shields.io/github/license/bohd4nx/TGgifts-buyer" alt="License">
  <img src="https://img.shields.io/github/stars/bohd4nx/TGgifts-buyer" alt="Stars">
  <img src="https://img.shields.io/github/forks/bohd4nx/TGgifts-buyer" alt="Forks">
  <img src="https://img.shields.io/github/issues/bohd4nx/TGgifts-buyer" alt="Issues">
  <br>
  <img src="https://img.shields.io/badge/developer-@bohd4nx-blue.svg" alt="Telegram">
  <img src="https://img.shields.io/badge/channel-@GiftsTracker-blue.svg" alt="Channel">
</p>

<p align="center">
  <a href="README.md">English</a> |
  <a href="./README-RU.md">Русский</a>
</p>

This user-bot automates the process of sending gifts to users on Telegram. The bot sends star gifts to specific users
and can be configured to handle both limited and non-limited gifts. It also manages a set of configurations that control
the bot's behavior, such as gift price limits, sender anonymity, and more.

## Table of Contents

- [General Configuration](#general-configuration)
- [Settings](#settings)
- [File and Data Paths](#file-and-data-paths)
- [Gifts & User Info](#gifts--user-info)
- [Custom Settings](#custom-settings)
- [How to Use](#how-to-use)
- [Issues](#issues)

## General Configuration

### `SESSION`

- **Description**: Path to the session file used by the bot to store session data.
- **Default**: `"data/account"`

### `API_ID` & `API_HASH`

- **Description**: Your Telegram API ID & HASH. You need to create an application
  on [Telegram's website](https://my.telegram.org/auth) to get this ID & HASH.

## Settings

### `INTERVAL`

- **Description**: The time interval (in seconds) between each check for new gifts.
- **Default**: `10` _Set it to at least 10 to avoid connection errors._

### `TIMEZONE`

- **Description**: The timezone in which the bot operates. This is used for time-based operations like logging.
- **Default**: `"Europe/Moscow"`

### `CHANNEL_ID`

- **Description**: The ID of the Telegram channel where the bot will send notifications about the gifts.

## File and Data Paths

### `DATA_FILEPATH`

- **Description**: Path to the file where the bot stores the history of sent gifts.
- **Default**: `"data/history.json"`

## Gifts & User Info

### `USER_ID`

- **Description**: List of user IDs that the bot will send gifts to. Users must have mutual contacts with the bot (i.e.,
  both the bot and the user must be in the same group or channel).

### `MIN_GIFT_PRICE`

- **Description**: The minimum price threshold for gifts. The bot will only send gifts above this price.
- **Default**: `0`

### `MAX_GIFT_PRICE`

- **Description**: The maximum allowed price for a gift. The bot will only send gifts below this price.
- **Default**: `100`

### `PURCHASE_NON_LIMITED_GIFTS`

- **Description**: If `True`, the bot will purchase non-limited gifts as they become available, respecting the maximum
  gift price.
- **Default**: `False`

### `HIDE_SENDER_NAME`

- **Description**: If `True`, the bot will hide its name when sending a gift to a user.
- **Default**: `True`

### `GIFT_IDS`

- **Description**: A list of specific gift IDs that the bot will send. This is optional, and if not specified, all
  available gifts may be sent.

### `NUM_GIFTS`

- **Description**: The number of gifts you want the bot to buy. **Same quantity for all gifts!**
- **Default**: `1`

### `GIFT_DELAY`

- **Description**: The delay (in seconds) between consecutive gift purchases to the same or different users.
- **Default**: `5` _Recommended to avoid rate limiting and detection._

## Custom Settings

### Making adjustments

- You can modify the values of `USER_ID`, `MAX_GIFT_PRICE`, and `PURCHASE_NON_LIMITED_GIFTS` directly in the config file
  to customize how and to whom the user-bot sends gifts.
- Set the `PURCHASE_NON_LIMITED_GIFTS` to `False` if you do not want the bot to automatically buy non-limited gifts.

## Localization

### Supported Languages

The bot supports multiple languages to make it accessible for users around the world. Localization ensures that
notifications and messages are sent in the preferred language of the user.

Currently, the bot supports the following languages:

- **English** (`locales/en.py`)
- **Ukrainian** (`locales/uk.py`)
- **Russian** (`locales/ru.py`)
- ~~**Spanish** (`locales/es.py`)~~
- ~~**Polish** (`locales/pl.py`)~~

### Configuration

Localization is managed through language files located in the `locales/` directory. To change the bot's language, you
can modify the `LANGUAGE` setting in the configuration.

Example:

```python
LANGUAGE = EN  # For English
```

### How to Add a New Language

To add a new language, follow these steps:

1. Create a new file in the `locales/` directory for the new language (e.g., `locales/fr.py` for French).
2. Define the translations in that file, following the format used in existing language files.
3. Add the language code to the `LANG_CODES` dictionary in the `config.py` file.

### Language File Example

Here is a basic structure of a language file (e.g., [
`locales/en.py`](https://github.com/bohd4nx/TGgifts-buyer/blob/main/locales/en.py):

```python
# -----------------------------
# Language Info (English)
# -----------------------------
LANG = "🇺🇸 English"
CODE = "EN-US"

# -----------------------------
# Telegram Messages
# -----------------------------
peer_id_error = ...

error_message = ...

balance_error = ...

usage_limited = ...

non_limited_error = ...

gift_price = ...
```

## How to Use

1. Clone or download the repository.
2. Ensure you have Python 3.10+ installed.
3. Install dependencies by running:

    ```bash
    pip install -r requirements.txt
    ```

4. Edit the `.env` file to include your API credentials, user IDs, gift IDs, and other relevant settings.
5. Run the bot:

    ```bash
    python main.py
    ```

The bot will start sending gifts according to the configuration. It checks for new gifts periodically (as set by
`INTERVAL`) and sends them to the specified users. Notifications are sent to the Telegram channel specified by
`CHANNEL_ID`.

## Issues

### №1

`AttributeError: 'Client' object has no attribute 'get_star_gifts'`

- **Description**: The bot encounters an `AttributeError` indicating that the `get_star_gifts` method is missing in the
  `Client` object.
- **Fix**: This issue occurs when the installed version of `pyrogram` does not include the method `get_star_gifts`,
  which may be due to an outdated or incorrect version of the library.

**Solution 1**: Reinstall the latest version of `pyrogram`:

Run the following command to reinstall `pyrogram`:

  ```bash
  pip uninstall pyrogram
  ```

  ```bash
  pip install pyrogram
  ```

  ```bash
  pip install pyrofork
  ```install pyrogram[pyrofork]
  ```

**Solution 2**: If the issue persists after upgrading, you can manually replace the `pyrogram` folder:

1. Download the [pyrogram.zip](https://github.com/user-attachments/files/17693486/pyrogram.zip).
2. Drag folders from the archive to the following path:

**Path to replace**:

  ```plaintext
  <your_project_directory>/venv/lib/pythonX.X/site-packages/pyrogram
  ```

or

  ```plaintext
  C:\Users\User\AppData\Local\Programs\Python\{Python Version}\Lib\site-packages
  ```

After replacing the folder, restart the bot and the problem should most likely be resolved.


---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ⚠️ Disclaimer

This bot is for educational purposes only. Use it at your own risk. The developers are not responsible for any misuse or
damage caused by this program.
