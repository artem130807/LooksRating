using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.Reviews.Command.CreateReview;
using LooksRatingApi.Enums;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetUserPhotos
{
    public class GetUserPhotosHandler : IRequestHandler<GetUserPhotosQuery, Result<GetUserPhotosResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IPhotoRecommendationService _photoRecommendationService;
        private readonly ISeasonRepository _seasonRepository;

        public GetUserPhotosHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoRecommendationService photoRecommendationService,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _photoRecommendationService = photoRecommendationService;
            _seasonRepository = seasonRepository;
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
            const int maxAttempts = 10;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
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
                    skipProfileIds.Add(profileIds[0]);
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
                    skipProfileIds.Add(profile.Id);
                    continue;
                }

                var response = await GetUserPhotosResponseBuilder.BuildAsync(
                    profile,
                    photos,
                    seasonIsClosed,
                    _photoProfileRepository,
                    cancellationToken);

                return Result.Success(response);
            }

            return Result.Failure<GetUserPhotosResponse>(GetUserPhotosErrors.NoPhotosAvailable);
        }
    }
}
