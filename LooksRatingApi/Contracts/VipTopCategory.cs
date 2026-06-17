using LooksRatingApi.Enums;

namespace LooksRatingApi.Contracts
{
    public sealed record VipTopCategory(
        Guid SeasonId,
        string City,
        GenderEnum Gender,
        int AgeBracket,
        IReadOnlyList<VipTopProfileCandidate> RankedProfiles);
}
