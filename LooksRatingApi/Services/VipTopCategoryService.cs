using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Enums;

namespace LooksRatingApi.Services
{
    public sealed class VipTopCategoryService : IVipTopCategoryService
    {
        private readonly IPhotoTopReadService _photoTopReadService;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ICityService _cityService;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ILogger<VipTopCategoryService> _logger;

        public VipTopCategoryService(
            IPhotoTopReadService photoTopReadService,
            IPhotoProfileRepository photoProfileRepository,
            ICityService cityService,
            ISeasonRepository seasonRepository,
            ILogger<VipTopCategoryService> logger)
        {
            _photoTopReadService = photoTopReadService;
            _photoProfileRepository = photoProfileRepository;
            _cityService = cityService;
            _seasonRepository = seasonRepository;
            _logger = logger;
        }

        public async Task<IReadOnlyList<VipTopCategory>> GetQualifiedCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("VIP-топ: сбор квалифицированных категорий");

            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                _logger.LogWarning("VIP-топ: текущий сезон не найден");
                return Array.Empty<VipTopCategory>();
            }

            var cities = _cityService.GetAllCities();
            if (cities.Count == 0)
            {
                _logger.LogWarning("VIP-топ: список городов пуст");
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
                        var (topProfileIds, totalCount) = await _photoTopReadService.GetTopProfileIdsAsync(
                            season.Id,
                            season.IsClosed,
                            city,
                            gender,
                            age,
                            skip: 0,
                            take: VipTopRules.TopCount,
                            vipOnly: true,
                            cancellationToken);

                        if (totalCount < VipTopRules.MinCategoryCount || topProfileIds.Count == 0)
                        {
                            continue;
                        }

                        categoryProfileIds.Add((city, gender, age, topProfileIds.ToList()));
                    }
                }
            }

            if (categoryProfileIds.Count == 0)
            {
                _logger.LogInformation(
                    "VIP-топ: нет категорий с минимум {MinCount} VIP-профилями (сезон {SeasonId})",
                    VipTopRules.MinCategoryCount,
                    season.Id);
                return Array.Empty<VipTopCategory>();
            }

            var allProfileIds = categoryProfileIds
                .SelectMany(entry => entry.ProfileIds)
                .Distinct()
                .ToList();

            var profiles = await _photoProfileRepository.GetByIdsAsync(allProfileIds, cancellationToken);
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

                if (ranked.Count < VipTopRules.MinCategoryCount)
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

                result.Add(new VipTopCategory(season.Id, city, gender, ageBracket, ranked));
            }

            _logger.LogInformation(
                "VIP-топ: найдено {CategoryCount} категорий, {ProfileCount} профилей (сезон {SeasonId})",
                result.Count,
                result.Sum(category => category.RankedProfiles.Count),
                season.Id);

            return result;
        }
    }
}
