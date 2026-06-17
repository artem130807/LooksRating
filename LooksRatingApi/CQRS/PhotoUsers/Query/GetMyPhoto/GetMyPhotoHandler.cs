using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using LooksRatingApi.Services;
using LooksRatingApi.Services.PhotoProfiles;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    public sealed class GetMyPhotoHandler : IRequestHandler<GetMyPhotoQuery, Result<GetMyPhotoResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;

        public GetMyPhotoHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
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

            var profile = await _photoProfileRepository.GetByUserAndSeasonAsync(
                user.Id,
                season.Id,
                cancellationToken);
            if (profile is null)
            {
                return Result.Failure<GetMyPhotoResponse>("PhotoNotFound");
            }

            return Result.Success(new GetMyPhotoResponse
            {
                ProfileId = profile.Id,
                UserId = user.Id,
                SeasonId = season.Id,
                PhotoCount = profile.Photos.Count,
                MaxPhotos = PhotoProfileLimits.GetMaxPhotos(user.Status),
                CanAddPhoto = PhotoProfileLimits.CanAddPhoto(profile.Photos.Count, user.Status),
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
