using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.SparksLedgerContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Enums;
using LooksRatingApi.Models;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Query.GetUserByTelegramId
{
    public sealed class GetUserByTelegramIdHandler
        : IRequestHandler<GetUserByTelegramIdQuery, Result<GetUserByTelegramIdResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly ISparksLedgerRepository _sparksLedgerRepository;

        public GetUserByTelegramIdHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository,
            ISparksLedgerRepository sparksLedgerRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _seasonRepository = seasonRepository;
            _sparksLedgerRepository = sparksLedgerRepository;
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
            List<PhotoProfile> photos = [];
            if (season is not null)
            {
                photos = await _photoProfileRepository.GetByTelegramAndSeasonListAsync(
                    request.TelegramId,
                    season.Id,
                    cancellationToken);
            }

            var settings = user.RecomendationSettings;
            var hasSettings = settings?.IsComplete == true;
            var sparksBalance = await _sparksLedgerRepository.GetBalanceAsync(user.Id, cancellationToken);

            return Result.Success(new GetUserByTelegramIdResponse
            {
                UserId = user.Id,
                TelegramId = user.TelegramId,
                TelegramUsername = user.TelegramUsername,
                CountInTop = user.CountInTop,
                DisplayName = UserPublicDisplayName.Resolve(user),
                Age = hasSettings ? settings!.Age : null,
                Gender = hasSettings ? settings!.Gender : Enums.GenderEnum.Unknown,
                City = hasSettings ? settings!.City.Value ?? string.Empty : string.Empty,
                HasRecommendationSettings = hasSettings,
                HasPhoto = photos.Count > 0,
                HasVip = user.Status == VipStatus.Availlable,
                SparksBalance = sparksBalance
            });
        }
    }
}
