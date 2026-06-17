using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.RecomendationSettings.Command.UpsertRecomendationSettings
{
    public sealed class UpsertRecomendationSettingsRequest
    {
        public long TelegramId { get; set; }
        public int Age { get; set; }
        public GenderEnum Gender { get; set; }
        public string City { get; set; } = string.Empty;
    }
}
