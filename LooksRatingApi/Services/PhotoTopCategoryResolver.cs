using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Services
{
    internal static class PhotoTopCategoryResolver
    {
        public static async Task<IReadOnlyList<VipTopCategory>> ResolveQualifiedCategoriesAsync(
            Guid seasonId,
            bool seasonIsClosed,
            bool vipOnly,
            int topCount,
            int minCategoryCount,
            IPhotoTopReadService photoTopReadService,
            IPhotoProfileRepository photoProfileRepository,
            ICityService cityService,
            CancellationToken cancellationToken)
        {
            var cities = cityService.GetAllCities();
            if (cities.Count == 0)
            {
                return Array.Empty<VipTopCategory>();
            }

            var categoryProfileIds = new List<(string City, GenderEnum Gender, int AgeBracket, List<Guid> ProfileIds)>();
            var ageBrackets = TopService.GetIntsList();
            var genders = new[] { GenderEnum.Male, GenderEnum.Female };

            foreach (var city in cities)
            {
                foreach (var ageBracket in ageBrackets)
                {
                    if (ageBracket.Length == 0)
                    {
                        continue;
                    }

                    var age = ageBracket[0];

                    foreach (var gender in genders)
                    {
                        var (topProfileIds, totalCount) = await photoTopReadService.GetTopProfileIdsAsync(
                            seasonId,
                            seasonIsClosed,
                            city,
                            gender,
                            age,
                            skip: 0,
                            take: topCount,
                            vipOnly,
                            cancellationToken);

                        if (totalCount < minCategoryCount || topProfileIds.Count == 0)
                        {
                            continue;
                        }

                        categoryProfileIds.Add((city, gender, age, topProfileIds.ToList()));
                    }
                }
            }

            if (categoryProfileIds.Count == 0)
            {
                return Array.Empty<VipTopCategory>();
            }

            var allProfileIds = categoryProfileIds
                .SelectMany(entry => entry.ProfileIds)
                .Distinct()
                .ToList();

            var profiles = await photoProfileRepository.GetByIdsAsync(allProfileIds, cancellationToken);
            var profileMap = profiles.ToDictionary(profile => profile.Id);

            var result = new List<VipTopCategory>();

            foreach (var (city, gender, ageBracket, profileIds) in categoryProfileIds)
            {
                var ranked = new List<VipTopProfileCandidate>();

                foreach (var profileId in profileIds)
                {
                    if (!profileMap.TryGetValue(profileId, out var profile))
                    {
                        continue;
                    }

                    if (!GenderFeedHelper.Matches(gender, profile.GenderNomination)
                        || !TopService.MatchesAge(ageBracket, profile.AgeNomination))
                    {
                        continue;
                    }

                    var telegramId = profile.User.TelegramId;
                    if (telegramId <= 0)
                    {
                        continue;
                    }

                    ranked.Add(new VipTopProfileCandidate(
                        telegramId,
                        profile.CityNomination.Value ?? city,
                        profile.Rating,
                        profile.RatingCount,
                        profile.AgeNomination,
                        profile.GenderNomination,
                        profile.CreatedAt));
                }

                if (ranked.Count < minCategoryCount)
                {
                    continue;
                }

                ranked.Sort((left, right) => PhotoRankingScore.Compare(
                    left.Rating,
                    left.RatingCount,
                    left.CreatedAt,
                    right.Rating,
                    right.RatingCount,
                    right.CreatedAt));

                result.Add(new VipTopCategory(seasonId, city, gender, ageBracket, ranked));
            }

            return result;
        }
    }
}
