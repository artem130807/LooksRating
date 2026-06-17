using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public sealed class GetTheBestWeekPhotosNowHandler
        : IRequestHandler<GetTheBestWeekPhotosNowQuery, Result<List<GetTheBestWeekPhotosNowResponse>>>
    {
        private readonly IGetTheBestWeekPhotosNowValidator _validator;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoTopReadService _photoTopReadService;

        public GetTheBestWeekPhotosNowHandler(
            IGetTheBestWeekPhotosNowValidator validator,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoTopReadService photoTopReadService)
        {
            _validator = validator;
            _photoProfileRepository = photoProfileRepository;
            _photoTopReadService = photoTopReadService;
        }

        public async Task<Result<List<GetTheBestWeekPhotosNowResponse>>> Handle(
            GetTheBestWeekPhotosNowQuery query,
            CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(query, cancellationToken);
            if (validationResult.IsFailure)
            {
                return Result.Failure<List<GetTheBestWeekPhotosNowResponse>>(validationResult.Error);
            }

            var context = validationResult.Value;
            var (profileIds, _) = await _photoTopReadService.GetTopProfileIdsAsync(
                context.CurrentSeason.Id,
                context.CurrentSeason.IsClosed,
                context.FeedCity,
                context.Query.Gender,
                context.Query.Age,
                skip: 0,
                take: GetTheBestWeekPhotosNowValidatedContext.TopPhotoCount,
                cancellationToken: cancellationToken);

            var profiles = await _photoProfileRepository.GetByIdsAsync(profileIds, cancellationToken);
            var profilesById = profiles.ToDictionary(p => p.Id);

            var items = new List<GetTheBestWeekPhotosNowResponse>(profileIds.Count);
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

                items.Add(new GetTheBestWeekPhotosNowResponse
                {
                    Id = profile.Id,
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
