using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.Services;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestVipPhotos
{
    public sealed class GetTheBestVipPhotosValidator : IGetTheBestVipPhotosValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetTheBestVipPhotosValidator(
            IUserRepository userRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<GetTheBestVipPhotosValidatedContext>> ValidateAsync(
            GetTheBestVipPhotosQuery query,
            CancellationToken cancellationToken)
        {
            if (query.TelegramId <= 0)
            {
                return Result.Failure<GetTheBestVipPhotosValidatedContext>(
                    SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (query.Age is < TopService.AllAges or > 100)
            {
                return Result.Failure<GetTheBestVipPhotosValidatedContext>(
                    GetTopUserPhotosErrors.InvalidAge);
            }

            var user = await _userRepository.GetUserByTelegramId(query.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetTheBestVipPhotosValidatedContext>(
                    SetUserPhotoErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings;
            if (settings is null || !settings.IsComplete)
            {
                return Result.Failure<GetTheBestVipPhotosValidatedContext>(
                    RecomendationSettingsErrors.SettingsNotFound);
            }

            var feedCity = settings.City?.Value;
            if (string.IsNullOrWhiteSpace(feedCity))
            {
                return Result.Failure<GetTheBestVipPhotosValidatedContext>(
                    RecomendationSettingsErrors.SettingsNotFound);
            }

            var currentSeason = await _seasonRepository.GetCurrent();
            if (currentSeason is null)
            {
                return Result.Failure<GetTheBestVipPhotosValidatedContext>(
                    GetTopUserPhotosErrors.SeasonNotFound);
            }

            return Result.Success(new GetTheBestVipPhotosValidatedContext
            {
                Query = query,
                CurrentSeason = currentSeason,
                FeedCity = feedCity.Trim().ToLowerInvariant(),
            });
        }
    }
}
