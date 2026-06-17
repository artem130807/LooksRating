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
        private readonly IPhotoProfileRepository _photoProfileRepository;
        private readonly IReviewRepository _reviewRepository;

        public CreateReviewValidator(
            IUserRepository userRepository,
            IPhotoProfileRepository photoProfileRepository,
            IReviewRepository reviewRepository)
        {
            _userRepository = userRepository;
            _photoProfileRepository = photoProfileRepository;
            _reviewRepository = reviewRepository;
        }

        public async Task<Result<string>> ValidateAsync(CreateReviewCommand command, CancellationToken cancellationToken)
        {
            if (command.ReviewerTelegramId <= 0)
            {
                return Result.Failure<string>(CreateReviewErrors.ReviewerTelegramIdIsRequired);
            }

            if (command.PhotoProfileId == Guid.Empty)
            {
                return Result.Failure<string>(CreateReviewErrors.PhotoProfileIdIsRequired);
            }
            var profileId = command.PhotoProfileId;

            if (command.Rating is < MinRating or > MaxRating)
            {
                return Result.Failure<string>(CreateReviewErrors.InvalidRatingValue);
            }

            var reviewer = await _userRepository.GetUserByTelegramId(command.ReviewerTelegramId);
            if (reviewer is null)
            {
                return Result.Failure<string>(CreateReviewErrors.ReviewerNotFound);
            }

            var photoProfile = await _photoProfileRepository.GetByIdAsync(profileId, cancellationToken);
            if (photoProfile is null)
            {
                return Result.Failure<string>(CreateReviewErrors.PhotoProfileNotFound);
            }

            if (photoProfile.UserId == reviewer.Id)
            {
                return Result.Failure<string>(CreateReviewErrors.SelfReviewIsNotAllowed);
            }

            var existingReview = await _reviewRepository.GetByUserAndProfileAsync(
                reviewer.Id,
                photoProfile.Id,
                cancellationToken);
            if (existingReview is not null)
            {
                return Result.Success(string.Empty);
            }

            return Result.Success(string.Empty);
        }
    }
}
