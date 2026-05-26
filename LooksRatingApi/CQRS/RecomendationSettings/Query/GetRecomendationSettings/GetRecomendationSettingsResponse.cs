using LooksRatingApi.Enums;

namespace LooksRatingApi.CQRS.RecomendationSettings.Query.GetRecomendationSettings
{
    public sealed class GetRecomendationSettingsResponse
    {
        public int? Age { get; init; }
        public GenderEnum Gender { get; init; }
        public string City { get; init; } = string.Empty;
        public bool IsConfigured { get; init; }
    }
}
