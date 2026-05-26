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
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IPhotoRecommendationService _photoRecommendationService;

        public GetUserPhotosHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            IPhotoRecommendationService photoRecommendationService)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
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

            var photoId = await _photoRecommendationService.GetNextUnratedPhotoIdAsync(
                user.Id,
                settings.Gender,
                settings.Age!.Value,
                feedCity);

            if (photoId is null)
            {
                return Result.Failure<GetUserPhotosResponse>(GetUserPhotosErrors.NoPhotosAvailable);
            }

            var photo = await _photoUserRepository.GePhotoUserById(photoId.Value);
            if (photo is null)
            {
                return Result.Failure<GetUserPhotosResponse>(CreateReviewErrors.PhotoUserNotFound);
            }

            if (photo.UserId == user.Id)
            {
                return Result.Failure<GetUserPhotosResponse>(CreateReviewErrors.SelfReviewIsNotAllowed);
            }

            return Result.Success(new GetUserPhotosResponse
            {
                Id = photo.Id,
                TelegramFileId = photo.TelegramFileId,
                Rank = RankDisplay.GetSticker(photo.Rank),
                Rating = photo.Rating,
                RatingCount = photo.RatingCount,
                UserId = photo.UserId,
                Gender = GenderDisplay.GetGender(photo.GenderNomination),
                City = photo.CityNomination.Value ?? string.Empty,
                DisplayName = UserPublicDisplayName.Resolve(photo.User),
            });
        }
    }
}
