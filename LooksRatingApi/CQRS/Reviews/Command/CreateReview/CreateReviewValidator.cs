using CSharpFunctionalExtensions;
using LooksRatingApi.Contracts.PhotoUserContracts;
using LooksRatingApi.Contracts.ReviewContracts;
using LooksRatingApi.Contracts.UserContracts;

namespace LooksRatingApi.Cqrs.Reviews.Command.CreateReview
{
    public sealed class CreateReviewValidator : ICreateReviewValidator
    {
        private const int MinRating = 1;
        private const int MaxRating = 10;

        private readonly IUserRepository _userRepository;
        private readonly IPhotoUserRepository _photoUserRepository;
        private readonly IReviewRepository _reviewRepository;

        public CreateReviewValidator(
            IUserRepository userRepository,
            IPhotoUserRepository photoUserRepository,
            IReviewRepository reviewRepository)
        {
            _userRepository = userRepository;
            _photoUserRepository = photoUserRepository;
            _reviewRepository = reviewRepository;
        }

        public async Task<Result<string>> ValidateAsync(CreateReviewCommand command, CancellationToken cancellationToken)
        {
            if (command.ReviewerTelegramId <= 0)
            {
                return Result.Failure<string>(CreateReviewErrors.ReviewerTelegramIdIsRequired);
            }

            if (command.PhotoUserId == Guid.Empty)
            {
                return Result.Failure<string>(CreateReviewErrors.PhotoUserIdIsRequired);
            }

            if (command.Rating is < MinRating or > MaxRating)
            {
                return Result.Failure<string>(CreateReviewErrors.InvalidRatingValue);
            }

            var reviewer = await _userRepository.GetUserByTelegramId(command.ReviewerTelegramId);
            if (reviewer is null)
            {
                return Result.Failure<string>(CreateReviewErrors.ReviewerNotFound);
            }

            var photoUser = await _photoUserRepository.GePhotoUserById(command.PhotoUserId);
            if (photoUser is null)
            {
                return Result.Failure<string>(CreateReviewErrors.PhotoUserNotFound);
            }

            if (photoUser.UserId == reviewer.Id)
            {
                return Result.Failure<string>(CreateReviewErrors.SelfReviewIsNotAllowed);
            }

            return Result.Success(string.Empty);
        }
    }
}
