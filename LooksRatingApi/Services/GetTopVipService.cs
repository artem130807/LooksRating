using LooksRatingApi.Contracts;

namespace LooksRatingApi.Services
{
    public sealed class GetTopVipService : IGetTopVipService
    {
        private readonly IVipTopCategoryService _vipTopCategoryService;

        public GetTopVipService(IVipTopCategoryService vipTopCategoryService)
        {
            _vipTopCategoryService = vipTopCategoryService;
        }

        public async Task<IReadOnlyList<VipTopProfileCandidate>> GetCandidates(
            CancellationToken cancellationToken = default)
        {
            var categories = await _vipTopCategoryService.GetQualifiedCategoriesAsync(cancellationToken);
            return categories
                .SelectMany(category => category.RankedProfiles)
                .ToList();
        }
    }
}
