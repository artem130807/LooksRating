using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Query.GetUserByTelegramId
{
    public sealed class GetUserByTelegramIdHandler
        : IRequestHandler<GetUserByTelegramIdQuery, Result<GetUserByTelegramIdResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetUserByTelegramIdHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<GetUserByTelegramIdResponse>> Handle(
            GetUserByTelegramIdQuery request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<GetUserByTelegramIdResponse>("TelegramIdIsRequired");
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetUserByTelegramIdResponse>("UserNotFound");
            }

            var season = await _seasonRepository.GetCurrent();
            PhotoUser? photo = null;
            if (season is not null)
            {
                photo = await _photoUserRepository.GetByTelegramIdAndSeasonIdAsync(
                    request.TelegramId,
                    season.Id,
                    cancellationToken);
            }

            var settings = user.RecomendationSettings;
            var hasSettings = settings?.IsComplete == true;

            return Result.Success(new GetUserByTelegramIdResponse
            {
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramUsername = user.TelegramUsername,
                DisplayName = UserPublicDisplayName.Resolve(user),
                Age = hasSettings ? settings!.Age : null,
                Gender = hasSettings ? settings!.Gender : Enums.GenderEnum.Unknown,
                City = hasSettings ? settings!.City.Value ?? string.Empty : string.Empty,
                HasRecommendationSettings = hasSettings,
                HasPhoto = photo is not null
            });
        }
    }
}
