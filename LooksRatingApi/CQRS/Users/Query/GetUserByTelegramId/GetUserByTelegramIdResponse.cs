using LooksRatingApi.Enums;



namespace LooksRatingApi.CQRS.Users.Query.GetUserByTelegramId

{

    public sealed class GetUserByTelegramIdResponse

    {
        public Guid UserId { get; init; }
        public long TelegramId { get; init; }
        public string? TelegramUsername { get; init; }
        public string DisplayName { get; init; } = string.Empty;
        public int CountInTop {get; init;}
        public int? Age { get; init; }
        public GenderEnum Gender { get; init; }
        public string City { get; init; } = string.Empty;
        public bool HasRecommendationSettings { get; init; }
        public bool HasPhoto { get; init; }
    }

}

