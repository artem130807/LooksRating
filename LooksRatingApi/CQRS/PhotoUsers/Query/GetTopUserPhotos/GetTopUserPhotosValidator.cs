using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.CQRS.RecomendationSettings;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos
{
    public sealed class GetTopUserPhotosValidator : IGetTopUserPhotosValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetTopUserPhotosValidator(
            IUserRepository userRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<GetTopUserPhotosValidatedContext>> ValidateAsync(
            GetTopUserPhotosQuery query,
            CancellationToken cancellationToken)
        {
            if (query.TelegramId <= 0)
            {
                return Result.Failure<GetTopUserPhotosValidatedContext>(
                    SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (query.Age is < 14 or > 100)
            {
                return Result.Failure<GetTopUserPhotosValidatedContext>(
                    GetTopUserPhotosErrors.InvalidAge);
            }

            var user = await _userRepository.GetUserByTelegramId(query.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetTopUserPhotosValidatedContext>(
                    SetUserPhotoErrors.UserNotFound);
            }

            var settings = user.RecomendationSettings;
            if (settings is null || !settings.IsComplete)
            {
                return Result.Failure<GetTopUserPhotosValidatedContext>(
                    RecomendationSettingsErrors.SettingsNotFound);
            }

            var feedCity = settings.City?.Value;
            if (string.IsNullOrWhiteSpace(feedCity))
            {
                return Result.Failure<GetTopUserPhotosValidatedContext>(
                    RecomendationSettingsErrors.SettingsNotFound);
            }

            var currentSeason = await _seasonRepository.GetCurrent();
            var season = query.SeasonId.HasValue && query.SeasonId.Value != Guid.Empty
                ? await _seasonRepository.GetById(query.SeasonId.Value)
                : currentSeason;

            if (season is null)
            {
                return Result.Failure<GetTopUserPhotosValidatedContext>(
                    GetTopUserPhotosErrors.SeasonNotFound);
            }

            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            return Result.Success(new GetTopUserPhotosValidatedContext
            {
                Query = query,
                Season = season,
                CurrentSeason = currentSeason,
                FeedCity = feedCity.Trim().ToLowerInvariant(),
                Page = page,
                PageSize = pageSize,
                Skip = (page - 1) * pageSize,
            });
        }
    }
}
