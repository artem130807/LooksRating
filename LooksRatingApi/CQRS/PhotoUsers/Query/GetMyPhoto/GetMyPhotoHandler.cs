using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    public sealed class GetMyPhotoHandler : IRequestHandler<GetMyPhotoQuery, Result<GetMyPhotoResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetMyPhotoHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<GetMyPhotoResponse>> Handle(
            GetMyPhotoQuery request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<GetMyPhotoResponse>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetMyPhotoResponse>(SetUserPhotoErrors.UserNotFound);
            }

            var season = await _seasonRepository.GetCurrent();
            if (season is null)
            {
                return Result.Failure<GetMyPhotoResponse>(SetUserPhotoErrors.CurrentSeasonNotFound);
            }

            var photo = await _photoUserRepository.GetByTelegramIdAndSeasonIdAsync(
                request.TelegramId,
                season.Id,
                cancellationToken);
            if (photo is null)
            {
                return Result.Failure<GetMyPhotoResponse>("PhotoNotFound");
            }

            return Result.Success(new GetMyPhotoResponse
            {
                Id = photo.Id,
                UserId = photo.UserId,
                TelegramFileId = photo.TelegramFileId,
                Rating = photo.Rating,
                RatingCount = photo.RatingCount,
                Rank = RankDisplay.GetSticker(photo.Rank),
                Gender = GenderDisplay.GetGender(photo.GenderNomination),
                Age = photo.AgeNomination,
                City = photo.CityNomination.Value ?? string.Empty,
                SeasonId = photo.SeasonId
            });
        }
    }
}
