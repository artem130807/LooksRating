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
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetMyPhotoBySeasonHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
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

            var profile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                user.Id,
                request.SeasonId,
                cancellationToken);
            if (profile is null)
            {
                return Result.Failure<GetMyPhotoResponse>("PhotoNotFound");
            }

            return Result.Success(new GetMyPhotoResponse
            {
                ProfileId = profile.Id,
                UserId = user.Id,
                SeasonId = request.SeasonId,
                Photos = profile.Photos
                    .OrderBy(x => x.SortOrder)
                    .Select(item => new GetMyPhotoItem
                    {
                        Id = item.Id,
                        TelegramFileId = item.TelegramFileId,
                        Rating = profile.Rating,
                        RatingCount = profile.RatingCount,
                        Rank = RankDisplay.GetSticker(profile.Rank),
                        Gender = GenderDisplay.GetGender(profile.GenderNomination),
                        Age = profile.AgeNomination,
                        City = profile.CityNomination.Value ?? string.Empty,
                    })
                    .ToList(),
            });
        }
    }
}
