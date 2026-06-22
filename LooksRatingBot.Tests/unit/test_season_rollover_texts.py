def test_format_season_rollover_notify_text() -> None:
    from bot import texts

    message = texts.format_season_rollover_notify_text("Потный июнь", "Обгоревший июль")

    assert "Потный июнь" in message
    assert "Обгоревший июль" in message
    assert "заверш" in message.lower()
    assert "добавьте фото" in message.lower()
