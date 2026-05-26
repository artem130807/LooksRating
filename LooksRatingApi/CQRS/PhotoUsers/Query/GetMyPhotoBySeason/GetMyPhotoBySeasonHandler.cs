using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhotoBySeason
{
    public sealed class GetMyPhotoBySeasonHandler
        : IRequestHandler<GetMyPhotoBySeasonQuery, Result<GetMyPhotoResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetMyPhotoBySeasonHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _seasonRepository = seasonRepository;
        }

        public async Task<Result<GetMyPhotoResponse>> Handle(
            GetMyPhotoBySeasonQuery request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<GetMyPhotoResponse>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            if (request.SeasonId == Guid.Empty)
            {
                return Result.Failure<GetMyPhotoResponse>("SeasonIdIsRequired");
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetMyPhotoResponse>(SetUserPhotoErrors.UserNotFound);
            }

            var season = await _seasonRepository.GetById(request.SeasonId);
            if (season is null)
            {
                return Result.Failure<GetMyPhotoResponse>("SeasonNotFound");
            }

            var photo = await _photoUserRepository.GetByTelegramIdAndSeasonIdAsync(
                request.TelegramId,
                request.SeasonId,
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
                SeasonId = photo.SeasonId,
            });
        }
    }
}
