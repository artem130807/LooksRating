using LooksRatingApi.Contracts;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;

namespace LooksRatingApi.Services
{
    public sealed class VipTopCategoryService : IVipTopCategoryService
    {
        private readonly IPhotoTopReadService _photoTopReadService;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ICityService _cityService;
        private readonly ISeasonRepository _seasonRepository;

        public VipTopCategoryService(
            IPhotoTopReadService photoTopReadService,
            IPhotoProfileRepository photoProfileRepository,
            ICityService cityService,
            ISeasonRepository seasonRepository)
        {
            _photoTopReadService = photoTopReadService;
            _photoProfileRepository = photoProfileRepository;
            _cityService = cityService;
            _seasonRepository = seasonRepository;
        }

        public async Task<IReadOnlyList<VipTopCategory>> GetQualifiedCategoriesAsync(
            CancellationToken cancellationToken = default)
        {
            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return Array.Empty<VipTopCategory>();
            }

            return await PhotoTopCategoryResolver.ResolveQualifiedCategoriesAsync(
                season.Id,
                season.IsClosed,
                vipOnly: true,
                VipTopRules.TopCount,
                VipTopRules.MinCategoryCount,
                _photoTopReadService,
                _photoProfileRepository,
                _cityService,
                cancellationToken);
        }
    }
}
