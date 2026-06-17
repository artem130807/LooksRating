using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;

namespace LooksRatingApi.Services
{
    public sealed class SeasonTopCategoryService : ISeasonTopCategoryService
    {
        private readonly IPhotoTopReadService _photoTopReadService;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ICityService _cityService;

        public SeasonTopCategoryService(
            IPhotoTopReadService photoTopReadService,
            IPhotoProfileRepository photoProfileRepository,
            ICityService cityService)
        {
            _photoTopReadService = photoTopReadService;
            _photoProfileRepository = photoProfileRepository;
            _cityService = cityService;
        }

        public Task<IReadOnlyList<VipTopCategory>> GetQualifiedCategoriesForSeasonAsync(
            Guid seasonId,
            bool seasonIsClosed,
            CancellationToken cancellationToken = default) =>
            PhotoTopCategoryResolver.ResolveQualifiedCategoriesAsync(
                seasonId,
                seasonIsClosed,
                vipOnly: false,
                SeasonTopRules.TopCount,
                SeasonTopRules.MinCategoryCount,
                _photoTopReadService,
                _photoProfileRepository,
                _cityService,
                cancellationToken);
    }
}
