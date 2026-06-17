using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos
{
    public sealed class GetTheBestVipPhotosHandler
        : IRequestHandler<GetTheBestVipPhotosQuery, Result<List<GetTheBestVipPhotosResponse>>>
    {
        private readonly IGetTheBestVipPhotosValidator _validator;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoTopReadService _photoTopReadService;

        public GetTheBestVipPhotosHandler(
            IGetTheBestVipPhotosValidator validator,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoTopReadService photoTopReadService)
        {
            _validator = validator;
            _photoProfileRepository = photoProfileRepository;
            _photoTopReadService = photoTopReadService;
        }

        public async Task<Result<List<GetTheBestVipPhotosResponse>>> Handle(
            GetTheBestVipPhotosQuery query,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<List<GetTheBestVipPhotosResponse>>(validationResult.Error);
            }

            var context = validationResult.Value;
            var (profileIds, _) = await _photoTopReadService.GetTopProfileIdsAsync(
                context.CurrentSeason.Id,
                context.CurrentSeason.IsClosed,
                context.FeedCity,
                context.Query.Gender,
                context.Query.Age,
                skip: 0,
                take: GetTheBestVipPhotosValidatedContext.TopPhotoCount,
                vipOnly: true,
                cancellationToken: cancellationToken);

            var profiles = await _photoProfileRepository.GetByIdsAsync(profileIds, cancellationToken);
            var profilesById = profiles.ToDictionary(p => p.Id);

            var items = new List<GetTheBestVipPhotosResponse>(profileIds.Count);
            foreach (var profileId in profileIds)
            {
                if (!profilesById.TryGetValue(profileId, out var profile))
                {
                    continue;
                }

                var files = profile.Photos
                
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.TelegramFileId)
                    .ToList();
                var firstFile = files.FirstOrDefault() ?? string.Empty;

                items.Add(new GetTheBestVipPhotosResponse
                {
                    Id = profile.Id,
                    TelegramId = profile.User.TelegramId,
                    ProfileId = profile.Id,
                    Place = items.Count + 1,
                    Name = UserPublicDisplayName.Resolve(profile.User),
                    TelegramFileId = firstFile,
                    TelegramFileIds = files,
                    Rating = profile.Rating,
                    RatingCount = profile.RatingCount,
                    GenderNomination = GenderDisplay.GetGender(profile.GenderNomination),
                    AgeNomination = profile.AgeNomination,
                });
            }

            return Result.Success(items);
        }
    }
}
