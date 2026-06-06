using LooksRatingApi.Contracts;

namespace LooksRatingApi.Services
{
    public sealed class VipTopRewardOrchestrator : IVipTopRewardOrchestrator
    {
        private readonly IVipTopCategoryService _vipTopCategoryService;
        private readonly IVipStatusExtensionService _vipStatusExtensionService;
        private readonly ILogger<VipTopRewardOrchestrator> _logger;

        public VipTopRewardOrchestrator(
            IVipTopCategoryService vipTopCategoryService,
            IVipStatusExtensionService vipStatusExtensionService,
            ILogger<VipTopRewardOrchestrator> logger)
        {
            _vipTopCategoryService = vipTopCategoryService;
            _vipStatusExtensionService = vipStatusExtensionService;
            _logger = logger;
        }

        public async Task<IReadOnlyList<VipTopProfileCandidate>> ProcessAndGetProfilesAsync(
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("VIP-топ награды: обработка запроса");

            var categories = await _vipTopCategoryService.GetQualifiedCategoriesAsync(cancellationToken);
            if (categories.Count == 0)
            {
                _logger.LogInformation("VIP-топ награды: квалифицированных категорий нет");
                return Array.Empty<VipTopProfileCandidate>();
            }

            var seasonId = categories[0].SeasonId;
            var extensionTelegramIds = VipTopPlacement.GetExtensionTelegramIds(categories);

            try
            {
                var extensionResult = await _vipStatusExtensionService.ExtendByTelegramIdsAsync(
                    extensionTelegramIds,
                    seasonId,
                    cancellationToken);

                _logger.LogInformation(
                    "VIP top extension: extended={Extended}, skipped={Skipped}, notFound={NotFound}, candidates={Candidates}",
                    extensionResult.Extended,
                    extensionResult.Skipped,
                    extensionResult.NotFound,
                    extensionTelegramIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "VIP top extension failed for season {SeasonId}, gift payload will still be returned",
                    seasonId);
            }

            return categories
                .SelectMany(category => category.RankedProfiles)
                .ToList();
        }
    }
}
