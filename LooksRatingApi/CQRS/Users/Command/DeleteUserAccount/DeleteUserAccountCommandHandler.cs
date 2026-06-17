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
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserSessionRepository _userSessionRepository;

        public DeleteUserAccountCommandHandler(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IReviewRepository reviewRepository,
            IUserSessionRepository userSessionRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _reviewRepository = reviewRepository;
            _userSessionRepository = userSessionRepository;
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

            var profiles = await _photoProfileRepository.GetByUserIdWithSeasonAsync(
                user.Id,
                cancellationToken);

            foreach (var profile in profiles)
            {
                await _photoProfileRepository.DeleteAsync(profile.Id, cancellationToken);
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
