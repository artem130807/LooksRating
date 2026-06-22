using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.TheBestWeekContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetTopUserPhotos;
using LooksRatingApi.CQRS.RecomendationSettings;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.TheBestWeeks.Query.GetTheBestWeeks
{
    public sealed class GetTheBestWeeksHandler
        : IRequestHandler<GetTheBestWeeksQuery, Result<List<GetTheBestWeeksResponse>>>
    {
        private readonly ITheBestWeekRepository _theBestWeekRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        public GetTheBestWeeksHandler(
            ITheBestWeekRepository theBestWeekRepository,
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository)
        {
            _theBestWeekRepository = theBestWeekRepository;
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
        }

        public async Task<Result<List<GetTheBestWeeksResponse>>> Handle(
            GetTheBestWeeksQuery query,
            CancellationToken cancellationToken)
        {
            if (!query.TelegramId.HasValue || query.TelegramId.Value <= 0)
            {
                return Result.Failure<List<GetTheBestWeeksResponse>>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (!TopService.IsValidFeedAge(query.Age))
            {
                return Result.Failure<List<GetTheBestWeeksResponse>>(GetTopUserPhotosErrors.InvalidAge);
            }

            var user = await _userRepository.GetUserByTelegramId(query.TelegramId.Value);
            if (user is null)
            {
                return Result.Failure<List<GetTheBestWeeksResponse>>("Пользователь не найден");
            }

            var settings = user.RecomendationSettings;
            if (settings is null || !settings.IsComplete || string.IsNullOrWhiteSpace(settings.City?.Value))
            {
                return Result.Failure<List<GetTheBestWeeksResponse>>(RecomendationSettingsErrors.SettingsNotFound);
            }

            var currentTheBestWeek = await _theBestWeekRepository.GetCurrentWeek();
            if (currentTheBestWeek is null)
            {
                return Result.Failure<List<GetTheBestWeeksResponse>>("Действующая неделя не найдена");
            }

            var profiles = await _photoProfileRepository.GetByCitySnapshotAsync(
                currentTheBestWeek.Id,
                settings.City!.Value,
                query.Age,
                query.Gender);

            var responses = new List<GetTheBestWeeksResponse>(profiles.Count);
            var place = 1;
            foreach (var profile in profiles)
            {
                var files = profile.Photos
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.TelegramFileId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList();

                responses.Add(new GetTheBestWeeksResponse
                {
                    Id = profile.Id,
                    ProfileId = profile.Id,
                    Place = place++,
                    TelegramUsername = profile.User?.TelegramUsername,
                    Name = UserPublicDisplayName.Resolve(profile.User),
                    TelegramFileId = files.FirstOrDefault() ?? string.Empty,
                    TelegramFileIds = files,
                    GenderNomination = GenderDisplay.GetGender(profile.GenderNomination),
                    AgeNomination = profile.AgeNomination,
                    Rating = profile.Rating,
                    RatingCount = profile.RatingCount
                });
            }

            return responses;
        }
    }
}
