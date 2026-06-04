# -----------------------------
# Language Info (Spanish)
# -----------------------------
LANG = "🇪🇸 Español"
CODE = "ES-ES"

# -----------------------------
# Telegram Messages
# -----------------------------
peer_id_error = (
    "<b>❗¡Error al enviar el regalo!</b>\n\n"
    "Asegúrese de que la inicialización del chat haya sido exitosa, "
    "que haya interactuado previamente con este usuario, <b><i>¡y que no esté enviando un regalo a sí mismo!</i></b>\n\n"
    "⚠️ Si no funciona, intente agregar al usuario a sus contactos o contacte al desarrollador: @B7XX7B"
)

error_message = "<b>❗¡Error al comprar el regalo!</b>\n\n<pre>{}</pre>"

balance_error = ("<b>🎁 Regalo</b> [<code>{}</code>] no se pudo enviar debido a saldo insuficiente!"
                 "\n<b>Recargue su saldo para continuar enviando regalos.</b>")

usage_limited = "<b>❗Regalo limitado</b> [<code>{}</code>] agotado."

non_limited_error = "<b>❗El regalo</b> [<code>{}</code>] no tiene límite. Se omite según su configuración..."

gift_price = "<b>🎁 El regalo</b> [<code>{}</code>] es demasiado caro: <b>{} ⭐</b>. Se omite..."

# message = f"<b>🎁 {f'{num} ' if num != 1 else ''}Regalo{'s' if num != 1 else ''}</b> " \
#           f"[<code>{star_gift_id}</code>] ha sido enviado con éxito!\n\n" \
#           f"<b>Destinatario:</b> "

# -----------------------------
# Console Messages
# -----------------------------
low_balance = "¡Saldo insuficiente de estrellas para enviar el regalo!"
out_of_stock = "Regalo limitado: {} fuera de stock."
peer_id = "¡Asegúrese de haber interactuado previamente con este usuario o de no estar enviando un regalo a sí mismo!"
gift_send_error = "No se pudo enviar el regalo: {} al usuario: {}"
gift_checking = "Comprobando nuevos regalos"
new_gifts = "Nuevos regalos encontrados:"
gift_expensive = "El regalo: \033[1m{}\033[0m es demasiado caro: {}★"
non_limited_gift = "El regalo: \033[1m{}\033[0m no tiene límite. Se omite..."
purchase_error = "Error al comprar el regalo \033[1m{}\033[0m para el usuario: \033[1m{}\033[0m"
terminated = "El programa ha terminado"
unexpected_error = "Ocurrió un error inesperado:"
