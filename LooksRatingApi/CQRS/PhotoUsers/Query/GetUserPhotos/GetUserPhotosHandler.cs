using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos
{
    public class GetUserPhotosHandler : IRequestHandler<GetUserPhotosQuery, Result<GetUserPhotosResponse>>
    {
        private const int MaxAttempts = 10;

        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoRecommendationService _photoRecommendationService;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoTopReadService _photoTopReadService;
        private readonly IUnviewablePhotosProfilesService _unviewablePhotosProfilesService;
        private readonly ILogger<GetUserPhotosHandler> _logger;

        public GetUserPhotosHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoRecommendationService photoRecommendationService,
            ISeasonRepository seasonRepository,
            IPhotoTopReadService photoTopReadService,
            IUnviewablePhotosProfilesService unviewablePhotosProfilesService,
            ILogger<GetUserPhotosHandler> logger)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _photoRecommendationService = photoRecommendationService;
            _seasonRepository = seasonRepository;
            _photoTopReadService = photoTopReadService;
            _unviewablePhotosProfilesService = unviewablePhotosProfilesService;
            _logger = logger;
        }

        public async Task<Result<GetUserPhotosResponse>> Handle(
            GetUserPhotosQuery request,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByTelegramId(request.telegramId);
            if (user is null)
            {
                return Result.Failure<GetUserPhotosResponse>("Пользователь не найдён");
            }

            var settings = user.RecomendationSettings;
            if (settings is null || !settings.IsComplete)
            {
                return Result.Failure<GetUserPhotosResponse>(GetUserPhotosErrors.RecommendationSettingsIncomplete);
            }

            var feedCity = settings.City?.Value;
            if (string.IsNullOrWhiteSpace(feedCity))
            {
                return Result.Failure<GetUserPhotosResponse>(GetUserPhotosErrors.RecommendationSettingsIncomplete);
            }

            var season = await _seasonRepository.GetCurrent();
            var seasonIsClosed = season?.IsClosed ?? false;

            var skipProfileIds = new HashSet<Guid>();
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var profileIds = await _photoRecommendationService.GetNextUnratedProfileIdsAsync(
                    user.Id,
                    settings.Gender,
                    settings.Age!.Value,
                    feedCity,
                    skipProfileIds: skipProfileIds);

                if (profileIds.Count == 0)
                {
                    break;
                }

                var profile = await _photoProfileRepository.GetByIdAsync(profileIds[0], cancellationToken);
                if (profile is null)
                {
                    await ExcludeFromUserFeedAsync(
                        profileIds[0],
                        user.Id,
                        skipProfileIds,
                        cancellationToken);
                    continue;
                }

                var photos = profile.Photos
                    .OrderBy(x => x.SortOrder)
                    .Select(photo => new GetUserPhotosItem
                    {
                        Id = photo.Id,
                        TelegramFileId = photo.TelegramFileId,
                    })
                    .Where(photo => !string.IsNullOrWhiteSpace(photo.TelegramFileId))
                    .ToList();

                if (photos.Count == 0)
                {
                    await ExcludeFromUserFeedAsync(
                        profile.Id,
                        user.Id,
                        skipProfileIds,
                        cancellationToken);
                    continue;
                }

                var response = await GetUserPhotosResponseBuilder.BuildAsync(
                    profile,
                    photos,
                    seasonIsClosed,
                    _photoTopReadService,
                    cancellationToken);

                return Result.Success(response);
            }

            return Result.Failure<GetUserPhotosResponse>(GetUserPhotosErrors.NoPhotosAvailable);
        }

        private async Task ExcludeFromUserFeedAsync(
            Guid profileId,
            Guid userId,
            HashSet<Guid> skipProfileIds,
            CancellationToken cancellationToken)
        {
            skipProfileIds.Add(profileId);

            var cacheResult = await _unviewablePhotosProfilesService.AddUnviewablePhotosProfile(
                profileId,
                userId,
                cancellationToken);
            if (cacheResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to persist unviewable feed profile {PhotoProfileId} for user {UserId}: {Error}",
                    profileId,
                    userId,
                    cacheResult.Error);
            }
        }
    }
}
