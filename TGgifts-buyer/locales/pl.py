# -----------------------------
# Language Info (Polish)
# -----------------------------
LANG = "🇵🇱 Polski"
CODE = "PL-PL"

# -----------------------------
# Telegram Messages
# -----------------------------
peer_id_error = (
    "<b>❗Błąd podczas wysyłania prezentu!</b>\n\n"
    "Upewnij się, że wiadomość inicjująca została wysłana pomyślnie, "
    "interagowałeś wcześniej z tym użytkownikiem, <b><i>i nie wysyłasz prezentu do siebie!</i></b>\n\n"
    "⚠️ Jeśli nic nie pomaga, spróbuj dodać go do kontaktów lub wyślij wiadomość do dewelopera: @B7XX7B"
)

error_message = "<b>❗Błąd podczas zakupu prezentu!</b>\n\n<pre>{}</pre>"

balance_error = ("<b>🎁 Prezent</b> [<code>{}</code>] nie mógł zostać wysłany z powodu niewystarczającego salda!"
                 "\n<b>Proszę doładować saldo, aby kontynuować wysyłanie prezentów.</b>")

usage_limited = "<b>❗Prezent ograniczony</b> [<code>{}</code>] Wyprzedany."

non_limited_error = "<b>❗Prezent</b> [<code>{}</code>] jest nieograniczony. Pomijanie zgodnie z ustawieniami użytkownika..."

gift_price = "<b>🎁 Prezent</b> [<code>{}</code>] jest zbyt drogi: <b>{} ⭐</b>. Pomijanie..."

# message = f"<b>🎁 {f'{num} ' if num != 1 else ''}Prezent{'y' if num % 10 in [2, 3, 4] and num not in [12, 13, 14] else 'ów'}</b> " \
#           f"[<code>{star_gift_id}</code>] został pomyślnie wysłany!\n\n" \
#           f"<b>Odbiorca:</b> "

# -----------------------------
# Console Messages
# -----------------------------
low_balance = "Brak wystarczającej liczby gwiazdek, aby wysłać prezent!"
out_of_stock = "Ograniczony prezent: {} Niedostępny."
peer_id = "Upewnij się, że wcześniej interagowałeś z tym użytkownikiem lub nie wysyłasz prezentu do siebie!"
gift_send_error = "Nie udało się wysłać prezentu: {} do użytkownika: {}"
gift_checking = "Sprawdzanie nowych prezentów"
new_gifts = "Znaleziono nowe prezenty:"
gift_expensive = "Prezent: \033[1m{}\033[0m jest za drogi: {}★"
non_limited_gift = "Prezent: \033[1m{}\033[0m nielimitowany. Pomijanie..."
purchase_error = "Błąd podczas zakupu prezentu \033[1m{}\033[0m dla użytkownika: \033[1m{}\033[0m"
terminated = "Program zakończył swoją pracę"
unexpected_error = "Wystąpił nieoczekiwany błąd:"
