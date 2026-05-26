using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.UserContracts;
using LooksRatingApi.Contracts.UserSessionContracts;
using LooksRatingApi.Services;
using MediatR;

namespace LooksRatingApi.CQRS.Users.Command.DeleteUserAccount
{
    public sealed class DeleteUserAccountCommandHandler
        : IRequestHandler<DeleteUserAccountCommand, Result<Unit>>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserSessionRepository _userSessionRepository;
        private readonly IPhotoUserLifecycleService _photoUserLifecycleService;

        public DeleteUserAccountCommandHandler(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            IReviewRepository reviewRepository,
            IUserSessionRepository userSessionRepository,
            IPhotoUserLifecycleService photoUserLifecycleService)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _reviewRepository = reviewRepository;
            _userSessionRepository = userSessionRepository;
            _photoUserLifecycleService = photoUserLifecycleService;
        }

        public async Task<Result<Unit>> Handle(
            DeleteUserAccountCommand request,
            CancellationToken cancellationToken)
        {
            if (request.TelegramId <= 0)
            {
                return Result.Failure<Unit>(DeleteUserAccountErrors.TelegramIdIsRequired);
            }

            var user = await _userRepository.GetUserByTelegramId(request.TelegramId);
            if (user is null)
            {
                return Result.Failure<Unit>(DeleteUserAccountErrors.UserNotFound);
            }

            var photoUsers = await _photoUserRepository.GetByUserIdWithSeasonAsync(
                user.Id,
                cancellationToken);

            foreach (var photoUser in photoUsers)
            {
                await _photoUserLifecycleService.RemoveAsync(
                    photoUser,
                    photoUser.Season,
                    cancellationToken);
            }

            await _reviewRepository.DeleteByUserIdAsync(user.Id, cancellationToken);
            await _userRepository.Delete(user.Id);

            var session = await _userSessionRepository.GetByTelegramIdForUpdateAsync(
                request.TelegramId,
                cancellationToken);

            if (session is not null)
            {
                session.ResetForReregistration();
                await _userSessionRepository.UpdateAsync(session, cancellationToken);
            }

            return Result.Success(Unit.Value);
        }
    }
}
