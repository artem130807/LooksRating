namespace LooksRatingApi.Services.SeasonLifecycle
{
    internal static class SeasonMonthNames
    {
        private static readonly IReadOnlyDictionary<int, string> Names = new Dictionary<int, string>
        {
            [1] = "Опухший январь 🎄",
            [2] = "Бледный февраль 🌨️",
            [3] = "Красный март 🌬️",
            [4] = "Мокрый апрель ☔",
            [5] = "Жмурки май 😑",
            [6] = "Потный июнь 💦",
            [7] = "Обгоревший июль 🔥",
            [8] = "Сплюснутый август 🛏️",
            [9] = "Школьный сентябрь 📚",
            [10] = "Простуженный октябрь 🤧",
            [11] = "Сонный ноябрь 🦉",
            [12] = "Кутаный декабрь 🧣"
        };

        public static string Get(int month) =>
            Names.TryGetValue(month, out var name) ? name : $"Сезон {month}";
    }
}
