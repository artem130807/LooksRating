using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.SeasonContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using MediatR;

namespace LooksRatingApi.CQRS.PhotoUsers.Query.GetMyPhoto
{
    public sealed class GetMyPhotoHandler : IRequestHandler<GetMyPhotoQuery, Result<GetMyPhotoResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IPhotoTopReadService _photoTopReadService;

        public GetMyPhotoHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            ISeasonRepository seasonRepository,
            IPhotoTopReadService photoTopReadService)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _seasonRepository = seasonRepository;
            _photoTopReadService = photoTopReadService;
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

            var response = await GetMyPhotoResponseBuilder.BuildAsync(
                profile,
                user,
                season.Id,
                season.IsClosed,
                _photoTopReadService,
                cancellationToken);

            return Result.Success(response);
        }
    }
}
