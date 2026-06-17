using LooksRatingApi.Contracts;

namespace LooksRatingApi.Services
{
    /// <summary>
    /// Read-only VIP top snapshot for legacy gRPC consumers.
    /// Biweekly rewards (sparks 1–5, VIP extension 6–10) are applied by <see cref="VipTopSparksRewardProcessor"/>.
    /// </summary>
    public sealed class VipTopRewardOrchestrator : IVipTopRewardOrchestrator
    {
        private readonly IVipTopCategoryService _vipTopCategoryService;

        public VipTopRewardOrchestrator(IVipTopCategoryService vipTopCategoryService)
        {
            _vipTopCategoryService = vipTopCategoryService;
        }

        public async Task<IReadOnlyList<VipTopProfileCandidate>> ProcessAndGetProfilesAsync(
            CancellationToken cancellationToken = default)
        {
            var categories = await _vipTopCategoryService.GetQualifiedCategoriesAsync(cancellationToken);
            if (categories.Count == 0)
            {
                return Array.Empty<VipTopProfileCandidate>();
            }

            return categories
                .SelectMany(category => category.RankedProfiles)
                .ToList();
        }
    }
}
