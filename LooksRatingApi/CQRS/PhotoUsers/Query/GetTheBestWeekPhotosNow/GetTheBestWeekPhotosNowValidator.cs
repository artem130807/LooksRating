using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos;
using LooksRatingApi.CQRS.RecomendationSettings;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTheBestWeekPhotosNow
{
    public sealed class GetTheBestWeekPhotosNowValidator : IGetTheBestWeekPhotosNowValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetTheBestWeekPhotosNowValidator(
            IUserRepository userRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<GetTheBestWeekPhotosNowValidatedContext>> ValidateAsync(
            GetTheBestWeekPhotosNowQuery query,
            CancellationToken cancellationToken)
        {
            if (query.TelegramId <= 0)
            {
                return Result.Failure<GetTheBestWeekPhotosNowValidatedContext>(
                    SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (query.Age is < 14 or > 100)
            {
                return Result.Failure<GetTheBestWeekPhotosNowValidatedContext>(
                    GetTopUserPhotosErrors.InvalidAge);
            }

            var user = await _userRepository.GetUserByTelegramId(query.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetTheBestWeekPhotosNowValidatedContext>(
                    SetUserPhotoErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings;
            if (settings is null || !settings.IsComplete)
            {
                return Result.Failure<GetTheBestWeekPhotosNowValidatedContext>(
                    RecomendationSettingsErrors.SettingsNotFound);
            }

            var feedCity = settings.City?.Value;
            if (string.IsNullOrWhiteSpace(feedCity))
            {
                return Result.Failure<GetTheBestWeekPhotosNowValidatedContext>(
                    RecomendationSettingsErrors.SettingsNotFound);
            }

            var currentSeason = await _seasonRepository.GetCurrent();
            if (currentSeason is null)
            {
                return Result.Failure<GetTheBestWeekPhotosNowValidatedContext>(
                    GetTopUserPhotosErrors.SeasonNotFound);
            }

            return Result.Success(new GetTheBestWeekPhotosNowValidatedContext
            {
                Query = query,
                CurrentSeason = currentSeason,
                FeedCity = feedCity.Trim().ToLowerInvariant(),
            });
        }
    }
}
