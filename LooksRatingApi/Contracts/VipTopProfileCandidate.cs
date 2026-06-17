using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts
{
    public sealed record VipTopProfileCandidate(
        long TelegramId,
        string City,
        decimal Rating,
        int RatingCount,
        int Age,
        GenderEnum Gender,
        DateTime CreatedAt);
}
