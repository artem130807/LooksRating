using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Cqrs.PhotoUsers.Command.SetUserPhoto;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Query.GetUserStats
{
    public sealed class GetUserStatsHandler : IRequestHandler<GetUserStatsQuery, Result<GetUserStatsResponse>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;

        public GetUserStatsHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
        }

        public async Task<Result<GetUserStatsResponse>> Handle(
            GetUserStatsQuery request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            
            {
                return Result.Failure<GetUserStatsResponse>(SetUserPhotoErrors.TelegramIdIsRequired);
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<GetUserStatsResponse>(SetUserPhotoErrors.UserNotFound);
            }

            var seasonsWithPhoto = await _photoUserRepository.CountSeasonsWithPhotoAsync(
                user.Id,
                cancellationToken);

            return Result.Success(new GetUserStatsResponse
            {
                TelegramId = user.TelegramId,
                CountInTop = user.CountInTop,
                SeasonsWithPhoto = seasonsWithPhoto,
            });
        }
    }
}
