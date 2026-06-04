using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
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

        public GetUserPhotosHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IPhotoRecommendationService photoRecommendationService)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _photoRecommendationService = photoRecommendationService;
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

            var profileIds = await _photoRecommendationService.GetNextUnratedProfileIdsAsync(
                user.Id,
                settings.Gender,
                settings.Age!.Value,
                feedCity);

            if (profileIds.Count == 0)
            {
                return Result.Failure<GetUserPhotosResponse>(GetUserPhotosErrors.NoPhotosAvailable);
            }

            var profile = await _photoProfileRepository.GetByIdAsync(profileIds[0], cancellationToken);
            if (profile is null)
            {
                return Result.Failure<GetUserPhotosResponse>(CreateReviewErrors.PhotoProfileNotFound);
            }

            return Result.Success(new GetUserPhotosResponse
            {
                ProfileId = profile.Id,
                Rank = RankDisplay.GetSticker(profile.Rank),
                Rating = profile.Rating,
                RatingCount = profile.RatingCount,
                UserId = profile.UserId,
                Gender = GenderDisplay.GetGender(profile.GenderNomination),
                Age = profile.AgeNomination,
                City = profile.CityNomination.Value ?? string.Empty,
                DisplayName = UserPublicDisplayName.Resolve(profile.User),
                Photos = profile.Photos
                    .OrderBy(x => x.SortOrder)
                    .Select(photo => new GetUserPhotosItem
                    {
                        Id = photo.Id,
                        TelegramFileId = photo.TelegramFileId
                    })
                    .ToList(),
            });
        }
    }
}
